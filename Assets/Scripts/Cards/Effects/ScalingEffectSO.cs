using System.Collections.Generic;
using UnityEngine;
using FourE.Commanders;
using FourE.Core;
using FourE.Players;

namespace FourE.Cards.Effects
{
    /// <summary>
    /// Effetto che scala in base a un conteggio di carte/tag: applica Note, azioni e pesca
    /// proporzionali. Usato da Ripasso, Riassunto, Appunti e Sabotaggio.
    /// </summary>
    [CreateAssetMenu(fileName = "ScalingEffect", menuName = "4E/Effects/Scaling", order = 5)]
    public sealed class ScalingEffectSO : CardEffectSO
    {
        [SerializeField] private CountSource _countSource = CountSource.OwnDiscardPile;
        [Tooltip("Tag usato dalle sorgenti 'WithTag'.")]
        [SerializeField] private CardTag _tagFilter = CardTag.None;

        [Header("Per ogni unità contata")]
        [Tooltip("Note da applicare ai comandanti bersaglio (negativo per debuff).")]
        [SerializeField] private int _notePerCount;
        [Tooltip("Azioni da concedere nel turno corrente.")]
        [SerializeField] private int _actionPerCount;
        [Tooltip("Carte da far pescare al giocatore attivo.")]
        [SerializeField] private int _drawPerCount;

        /// <inheritdoc/>
        public override void Apply(GameContext context)
        {
            int count = ComputeCount(context);
            if (count <= 0)
            {
                return;
            }

            if (_notePerCount != 0)
            {
                int delta = _notePerCount * count;
                IEnumerable<CommanderState> commanders = _notePerCount < 0
                    ? context.ResolveDebuffCommanders(Target)
                    : context.ResolveCommanders(Target);
                foreach (CommanderState commander in commanders)
                {
                    context.RegisterChange(new InstantNoteChange(commander, delta));
                }
            }

            if (_actionPerCount != 0 && context.State?.Turns != null)
            {
                context.RegisterChange(new GrantActionsChange(context.State.Turns, _actionPerCount * count));
            }

            if (_drawPerCount != 0)
            {
                context.RegisterChange(new DrawCardsChange(context.ActivePlayer, _drawPerCount * count));
            }
        }

        /// <summary>
        /// Calcola il conteggio della sorgente selezionata.
        /// </summary>
        /// <param name="context">Contesto di gioco.</param>
        /// <returns>Numero di unità da cui scalare l'effetto.</returns>
        private int ComputeCount(GameContext context)
        {
            PlayerState own = context.ActivePlayer;
            PlayerState enemy = context.InactivePlayer;

            return _countSource switch
            {
                CountSource.OwnDiscardPile => own.DiscardPile.Count,
                CountSource.OwnHand => own.Hand.Count,
                CountSource.OwnDiscardDistinctTags => CountDistinctTags(own),
                CountSource.OwnDiscardWithTag => CountWithTag(own),
                CountSource.EnemyDiscardWithTag => CountWithTag(enemy),
                CountSource.BothDiscardWithTag => CountWithTag(own) + CountWithTag(enemy),
                CountSource.RemainingActionsAfterCurrentCard => RemainingActionsAfterCurrentCard(context),
                CountSource.OwnHandDistinctTags => CountDistinctHandTags(context),
                _ => 0
            };
        }

        /// <summary>
        /// Conta i tag distinti nella mano dopo la giocata, escludendo la carta in risoluzione
        /// che viene rimossa fisicamente dalla mano solo al termine del resolver.
        /// </summary>
        /// <param name="context">Contesto della carta in risoluzione.</param>
        /// <returns>Numero di tag distinti delle carte rimaste in mano.</returns>
        private static int CountDistinctHandTags(GameContext context)
        {
            CardTag union = CardTag.None;
            bool skippedPlayedCard = false;
            foreach (CardDataSO card in context.ActivePlayer.Hand)
            {
                if (!skippedPlayedCard && card == context.Card)
                {
                    skippedPlayedCard = true;
                    continue;
                }

                if (card != null)
                {
                    union |= card.Tags;
                }
            }

            return CountBits(union);
        }

        /// <summary>
        /// Calcola le azioni residue dopo aver considerato il costo della carta corrente.
        /// </summary>
        /// <param name="context">Contesto della carta in risoluzione.</param>
        /// <returns>Numero di azioni che resteranno disponibili, mai negativo.</returns>
        private static int RemainingActionsAfterCurrentCard(GameContext context)
        {
            int remaining = context.State?.Turns?.RemainingActions ?? 0;
            int actionCost = context.Card?.ActionCost ?? 0;
            return Mathf.Max(0, remaining - actionCost);
        }

        /// <summary>
        /// Conta le carte del cimitero che possiedono il tag filtro.
        /// </summary>
        /// <param name="player">Giocatore proprietario del cimitero.</param>
        /// <returns>Numero di carte col tag.</returns>
        private int CountWithTag(PlayerState player)
        {
            int count = 0;
            foreach (CardDataSO card in player.DiscardPile)
            {
                if (card != null && card.HasTag(_tagFilter))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Conta i tag distinti presenti nel cimitero del giocatore.
        /// </summary>
        /// <param name="player">Giocatore proprietario del cimitero.</param>
        /// <returns>Numero di tag distinti.</returns>
        private static int CountDistinctTags(PlayerState player)
        {
            return CountDistinctTags(player.DiscardPile);
        }

        /// <summary>
        /// Conta i tag distinti presenti nella raccolta indicata.
        /// </summary>
        /// <param name="cards">Carte da analizzare.</param>
        /// <returns>Numero di tag distinti.</returns>
        private static int CountDistinctTags(IEnumerable<CardDataSO> cards)
        {
            CardTag union = CardTag.None;
            foreach (CardDataSO card in cards)
            {
                if (card != null)
                {
                    union |= card.Tags;
                }
            }

            return CountBits(union);
        }

        /// <summary>Conta i bit attivi in un insieme di tag.</summary>
        private static int CountBits(CardTag tags)
        {
            int bits = 0;
            int value = (int)tags;
            while (value != 0)
            {
                bits += value & 1;
                value >>= 1;
            }

            return bits;
        }
    }
}
