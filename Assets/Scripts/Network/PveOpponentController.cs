using System;
using System.Collections.Generic;
using FourE.Cards;
using FourE.Config;
using FourE.Core;
using FourE.Players;

namespace FourE.Network
{
    /// <summary>
    /// Strategia dell'avversario PvE. Legge lo stato autoritativo e produce normali
    /// <see cref="GameIntent"/>, lasciando validazione e risoluzione ai manager di gioco.
    /// </summary>
    public sealed class PveOpponentController
    {
        private const int PreferredVerificaTurn = 4;
        private const int ForcedVerificaTurn = 8;

        private readonly GameStateManager _state;
        private readonly CardRegistry _registry;
        private readonly Random _random;
        private int _trackedShopRound = -1;
        private int _shopPurchaseAttempts;
        private bool _shopFinished;

        /// <summary>
        /// Crea il controller del computer.
        /// </summary>
        /// <param name="state">Stato autoritativo della partita.</param>
        /// <param name="registry">Registry usato per convertire le carte in id di intent.</param>
        /// <param name="random">Generatore casuale per spareggi tra scelte equivalenti.</param>
        public PveOpponentController(GameStateManager state, CardRegistry registry, Random random)
        {
            _state = state;
            _registry = registry;
            _random = random;
        }

        /// <summary>
        /// Indica se lo stato corrente richiede una decisione del computer.
        /// </summary>
        public bool CanAct
        {
            get
            {
                if (_state?.Player1 == null)
                {
                    return false;
                }

                return _state.CurrentPhase == GamePhase.Play
                    ? _state.ActivePlayer == _state.Player1
                    : _state.CurrentPhase == GamePhase.Shop
                      && (_trackedShopRound != _state.CurrentRoundIndex || !_shopFinished);
            }
        }

        /// <summary>
        /// Prova a creare la prossima azione del computer.
        /// </summary>
        /// <param name="intent">Intent prodotto, se esiste un'azione valida.</param>
        /// <returns>True quando è stata scelta un'azione.</returns>
        public bool TryCreateIntent(out GameIntent intent)
        {
            intent = default;
            if (_state?.Player1 == null)
            {
                return false;
            }

            if (_state.CurrentPhase == GamePhase.Shop)
            {
                return TryCreateShopIntent(out intent);
            }

            return _state.CurrentPhase == GamePhase.Play && _state.ActivePlayer == _state.Player1
                && TryCreatePlayIntent(out intent);
        }

        /// <summary>
        /// Decide se giocare una carta, chiudere con Verifica o terminare il turno.
        /// </summary>
        private bool TryCreatePlayIntent(out GameIntent intent)
        {
            PlayerState computer = _state.Player1;
            PlayerState human = _state.Player0;
            CardDataSO verifica = FindVerifica(computer);
            List<CardDataSO> playableCards = FindPlayableCards(computer);

            bool shouldPlayVerifica = verifica != null
                && _state.Turns.CanPlayVerificaThisTurn
                && !computer.VerificaBlocked
                && (_state.Turns.TurnInRound >= ForcedVerificaTurn
                    || playableCards.Count == 0
                    || (_state.Turns.TurnInRound >= PreferredVerificaTurn
                        && computer.TotalNotes >= human.TotalNotes));

            if (shouldPlayVerifica)
            {
                intent = GameIntent.PlayVerifica(computer.ActorNumber);
                return true;
            }

            CardDataSO selectedCard = SelectBestCard(playableCards);
            if (selectedCard != null)
            {
                BuildTargets(selectedCard, computer, human, out int[] actors, out int[] indices);
                intent = GameIntent.PlayCard(
                    computer.ActorNumber,
                    _registry.GetId(selectedCard),
                    actors,
                    indices);
                return true;
            }

            intent = GameIntent.EndTurn(computer.ActorNumber);
            return true;
        }

        /// <summary>
        /// Compra la carta più costosa accessibile, poi conclude lo shop.
        /// </summary>
        private bool TryCreateShopIntent(out GameIntent intent)
        {
            PlayerState computer = _state.Player1;
            if (_trackedShopRound != _state.CurrentRoundIndex)
            {
                _trackedShopRound = _state.CurrentRoundIndex;
                _shopPurchaseAttempts = 0;
                _shopFinished = false;
            }

            if (_shopFinished)
            {
                intent = default;
                return false;
            }

            CardDataSO purchase = SelectBestPurchase(computer);
            if (purchase != null
                && _shopPurchaseAttempts < _state.GameConfig.ShopPurchasesPerRound)
            {
                _shopPurchaseAttempts++;
                intent = GameIntent.BuyCard(computer.ActorNumber, _registry.GetId(purchase));
                return true;
            }

            _shopFinished = true;
            intent = GameIntent.FinishShop(computer.ActorNumber);
            return true;
        }

        /// <summary>
        /// Restituisce le carte standard che il computer può pagare con le azioni rimaste.
        /// </summary>
        private List<CardDataSO> FindPlayableCards(PlayerState computer)
        {
            List<CardDataSO> cards = new();
            foreach (CardDataSO card in computer.Hand)
            {
                if (card != null
                    && !card.IsVerifica
                    && card.ActionCost <= _state.Turns.RemainingActions)
                {
                    cards.Add(card);
                }
            }

            return cards;
        }

        /// <summary>
        /// Sceglie la carta con il valore strategico maggiore, randomizzando gli spareggi.
        /// </summary>
        private CardDataSO SelectBestCard(List<CardDataSO> cards)
        {
            CardDataSO best = null;
            int bestScore = int.MinValue;
            foreach (CardDataSO card in cards)
            {
                int score = ScoreCard(card) + _random.Next(0, 3);
                if (score > bestScore)
                {
                    best = card;
                    bestScore = score;
                }
            }

            return best;
        }

        /// <summary>
        /// Valuta una carta in base a costo azione e famiglie di effetti.
        /// </summary>
        private static int ScoreCard(CardDataSO card)
        {
            int score = 12 - (card.ActionCost * 2);
            foreach (CardEffectSO effect in card.Effects)
            {
                if (effect == null)
                {
                    continue;
                }

                score += effect.GetType().Name switch
                {
                    "BuffEffectSO" => 8,
                    "DebuffEffectSO" => 8,
                    "ScalingEffectSO" => 9,
                    "DrawEffectSO" => 7,
                    "ExtraActionEffectSO" => 7,
                    "BlockVerificaEffectSO" => 6,
                    "ForceDiscardEffectSO" => 6,
                    "ShieldEffectSO" => 5,
                    "ImmunityEffectSO" => 5,
                    "SwapNotesEffectSO" => 4,
                    "EqualizeNotesEffectSO" => 4,
                    _ => 3
                };
            }

            return score;
        }

        /// <summary>
        /// Costruisce bersagli coerenti con i requisiti della carta scelta.
        /// </summary>
        private static void BuildTargets(
            CardDataSO card,
            PlayerState computer,
            PlayerState human,
            out int[] actorNumbers,
            out int[] commanderIndices)
        {
            int ownIndex = IndexOfLowestCommander(computer);
            int enemyIndex = IndexOfHighestCommander(human);

            if (card.RequiresOrderedAnyTargetSelection)
            {
                actorNumbers = new[] { computer.ActorNumber, human.ActorNumber };
                commanderIndices = new[] { ownIndex, enemyIndex };
                return;
            }

            if (card.RequiresEnemyTargetSelection && card.RequiresOwnTargetSelection)
            {
                actorNumbers = new[] { human.ActorNumber, computer.ActorNumber };
                commanderIndices = new[] { enemyIndex, ownIndex };
                return;
            }

            if (card.RequiresEnemyTargetSelection)
            {
                actorNumbers = new[] { human.ActorNumber };
                commanderIndices = new[] { enemyIndex };
                return;
            }

            if (card.RequiresOwnTargetSelection)
            {
                actorNumbers = new[] { computer.ActorNumber };
                commanderIndices = new[] { ownIndex };
                return;
            }

            if (card.RequiresAnyTargetSelection)
            {
                bool hostile = PrefersEnemyTarget(card);
                actorNumbers = new[] { hostile ? human.ActorNumber : computer.ActorNumber };
                commanderIndices = new[] { hostile ? enemyIndex : ownIndex };
                return;
            }

            actorNumbers = Array.Empty<int>();
            commanderIndices = Array.Empty<int>();
        }

        /// <summary>
        /// Determina se una selezione libera è più utile su un bersaglio avversario.
        /// </summary>
        private static bool PrefersEnemyTarget(CardDataSO card)
        {
            foreach (CardEffectSO effect in card.Effects)
            {
                string effectName = effect?.GetType().Name;
                if (effectName == "DebuffEffectSO" || effectName == "BlockVerificaEffectSO")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Trova l'indice del secchione con meno Note.
        /// </summary>
        private static int IndexOfLowestCommander(PlayerState player)
        {
            return player.Commanders[GameConstants.FirstCommanderIndex].CurrentNote
                   <= player.Commanders[GameConstants.SecondCommanderIndex].CurrentNote
                ? GameConstants.FirstCommanderIndex
                : GameConstants.SecondCommanderIndex;
        }

        /// <summary>
        /// Trova l'indice del secchione con più Note.
        /// </summary>
        private static int IndexOfHighestCommander(PlayerState player)
        {
            return player.Commanders[GameConstants.FirstCommanderIndex].CurrentNote
                   >= player.Commanders[GameConstants.SecondCommanderIndex].CurrentNote
                ? GameConstants.FirstCommanderIndex
                : GameConstants.SecondCommanderIndex;
        }

        /// <summary>
        /// Cerca la Verifica nella mano del computer.
        /// </summary>
        private static CardDataSO FindVerifica(PlayerState player)
        {
            foreach (CardDataSO card in player.Hand)
            {
                if (card != null && card.IsVerifica)
                {
                    return card;
                }
            }

            return null;
        }

        /// <summary>
        /// Seleziona nello shop la carta accessibile dal costo maggiore.
        /// </summary>
        private static CardDataSO SelectBestPurchase(PlayerState computer)
        {
            CardDataSO best = null;
            foreach (CardDataSO card in computer.ShopPool)
            {
                if (card != null
                    && card.ShopCost <= computer.Credits
                    && (best == null || card.ShopCost > best.ShopCost))
                {
                    best = card;
                }
            }

            return best;
        }
    }
}
