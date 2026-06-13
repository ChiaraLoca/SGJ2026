using System.IO;

namespace FourE.Network
{
    /// <summary>
    /// Serializza <see cref="GameIntent"/> e <see cref="GameStateDTO"/> in <c>byte[]</c> compatti
    /// e viceversa. Indipendente da Photon: il trasporto si limita a trasmettere i byte.
    /// Entrambe le estremità usano questa stessa codifica, così host e client restano allineati.
    /// </summary>
    public static class NetworkSerializer
    {
        /// <summary>
        /// Serializza un intent in un array di byte.
        /// </summary>
        /// <param name="intent">Intent da serializzare.</param>
        /// <returns>Rappresentazione binaria dell'intent.</returns>
        public static byte[] SerializeIntent(GameIntent intent)
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write((byte)intent.Type);
            writer.Write(intent.ActorNumber);
            writer.Write(intent.CardId);
            WriteIntArray(writer, intent.TargetActorNumbers);
            WriteIntArray(writer, intent.TargetCommanderIndices);

            return stream.ToArray();
        }

        /// <summary>
        /// Ricostruisce un intent dai byte prodotti da <see cref="SerializeIntent"/>.
        /// </summary>
        /// <param name="data">Byte ricevuti dalla rete.</param>
        /// <returns>Intent deserializzato.</returns>
        public static GameIntent DeserializeIntent(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            IntentType type = (IntentType)reader.ReadByte();
            int actorNumber = reader.ReadInt32();
            int cardId = reader.ReadInt32();
            int[] targetActors = ReadIntArray(reader);
            int[] targetIndices = ReadIntArray(reader);

            return new GameIntent(type, actorNumber, cardId, targetActors, targetIndices);
        }

        /// <summary>
        /// Serializza uno snapshot di stato in un array di byte.
        /// </summary>
        /// <param name="state">Stato da serializzare.</param>
        /// <returns>Rappresentazione binaria dello stato.</returns>
        public static byte[] SerializeState(GameStateDTO state)
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(state.Phase);
            writer.Write(state.RoundIndex);
            writer.Write(state.ActiveActorNumber);
            writer.Write(state.IsGameOver);
            writer.Write(state.WinnerActorNumber);
            writer.Write(state.IsDraw);

            PlayerDTO[] players = state.Players ?? System.Array.Empty<PlayerDTO>();
            writer.Write(players.Length);
            foreach (PlayerDTO player in players)
            {
                WritePlayer(writer, player);
            }

            return stream.ToArray();
        }

        /// <summary>
        /// Ricostruisce uno snapshot di stato dai byte prodotti da <see cref="SerializeState"/>.
        /// </summary>
        /// <param name="data">Byte ricevuti dalla rete.</param>
        /// <returns>Stato deserializzato.</returns>
        public static GameStateDTO DeserializeState(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            GameStateDTO state = new()
            {
                Phase = reader.ReadInt32(),
                RoundIndex = reader.ReadInt32(),
                ActiveActorNumber = reader.ReadInt32(),
                IsGameOver = reader.ReadBoolean(),
                WinnerActorNumber = reader.ReadInt32(),
                IsDraw = reader.ReadBoolean()
            };

            int playerCount = reader.ReadInt32();
            state.Players = new PlayerDTO[playerCount];
            for (int i = 0; i < playerCount; i++)
            {
                state.Players[i] = ReadPlayer(reader);
            }

            return state;
        }

        /// <summary>Scrive un giocatore e i suoi comandanti.</summary>
        private static void WritePlayer(BinaryWriter writer, PlayerDTO player)
        {
            writer.Write(player.ActorNumber);
            writer.Write(player.Credits);
            writer.Write(player.Notes);
            writer.Write(player.DeckCount);
            WriteIntArray(writer, player.HandCardIds);
            WriteIntArray(writer, player.ShopPoolCardIds);

            CommanderDTO[] commanders = player.Commanders ?? System.Array.Empty<CommanderDTO>();
            writer.Write(commanders.Length);
            foreach (CommanderDTO commander in commanders)
            {
                writer.Write(commander.BaseNote);
                writer.Write(commander.CurrentNote);
                writer.Write(commander.HasDebuff);
                writer.Write(commander.ActiveBuffCount);
                writer.Write(commander.ActiveDebuffCount);
            }
        }

        /// <summary>Legge un giocatore e i suoi comandanti.</summary>
        private static PlayerDTO ReadPlayer(BinaryReader reader)
        {
            PlayerDTO player = new()
            {
                ActorNumber = reader.ReadInt32(),
                Credits = reader.ReadInt32(),
                Notes = reader.ReadInt32(),
                DeckCount = reader.ReadInt32(),
                HandCardIds = ReadIntArray(reader),
                ShopPoolCardIds = ReadIntArray(reader)
            };

            int commanderCount = reader.ReadInt32();
            player.Commanders = new CommanderDTO[commanderCount];
            for (int i = 0; i < commanderCount; i++)
            {
                player.Commanders[i] = new CommanderDTO
                {
                    BaseNote = reader.ReadInt32(),
                    CurrentNote = reader.ReadInt32(),
                    HasDebuff = reader.ReadBoolean(),
                    ActiveBuffCount = reader.ReadInt32(),
                    ActiveDebuffCount = reader.ReadInt32()
                };
            }

            return player;
        }

        /// <summary>Scrive un array di interi prefissato dalla lunghezza.</summary>
        private static void WriteIntArray(BinaryWriter writer, int[] values)
        {
            values ??= System.Array.Empty<int>();
            writer.Write(values.Length);
            foreach (int value in values)
            {
                writer.Write(value);
            }
        }

        /// <summary>Legge un array di interi prefissato dalla lunghezza.</summary>
        private static int[] ReadIntArray(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            int[] values = new int[length];
            for (int i = 0; i < length; i++)
            {
                values[i] = reader.ReadInt32();
            }

            return values;
        }
    }
}
