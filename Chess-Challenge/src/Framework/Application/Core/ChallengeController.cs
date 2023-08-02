using ChessChallenge.Chess;
using System;
using System.IO;
using System.Text;
using static ChessChallenge.Application.Settings;
using static ChessChallenge.Application.ConsoleHelper;

namespace ChessChallenge.Application
{
    public class ChallengeController
    {
        public enum PlayerType
        {
            Human,
            MyBot,
            EvilBot,
            OldBot,
        }

        // Bot match state
        readonly string[] botMatchStartFens;
        int botMatchGameIndex;
        public BotMatchStats BotStatsA { get; private set; }
        public BotMatchStats BotStatsB { get; private set; }
        public bool HumanWasWhiteLastGame { get; private set; }

        // Other
        public readonly BoardUI boardUI;
        readonly int tokenCount;
        readonly StringBuilder pgns;
        public bool fastForward;

        public int totalMovesPlayed = 0;
        public int totalGamesPlayed = 0;
        public int gamesPlayedThisMatch = 0;

        public GameThread[] gameThreads;
        public GameThread currentThread;

        PlayerType playerAType;
        PlayerType playerBType;

        public ChallengeController()
        {
            Log($"Launching Chess-Challenge version {Settings.Version}");
            tokenCount = GetTokenCount();
            Warmer.Warm();

            boardUI = new BoardUI();
            pgns = new();
            fastForward = false;

            BotStatsA = new BotMatchStats("IBot");
            BotStatsB = new BotMatchStats("IBot");
            botMatchStartFens = FileHelper.ReadResourceFile("Fens.txt").Split('\n');

            gameThreads = new GameThread[Settings.NumThreads];
            for (int i = 0; i < gameThreads.Length; ++i)
            {
                gameThreads[i] = new GameThread(this);
            }
            StartNewMatch(PlayerType.Human, PlayerType.MyBot);
        }

        void StartNewGame(GameThread thread)
        {
            bool isGameWithHuman = playerAType is PlayerType.Human || playerBType is PlayerType.Human;
            int fenIndex = isGameWithHuman ? 0 : botMatchGameIndex / 2;

            bool botAPlaysWhite = botMatchGameIndex % 2 == 0;
            PlayerType typeA = botAPlaysWhite ? playerAType : playerBType;
            PlayerType typeB = botAPlaysWhite ? playerBType : playerAType;

            thread.StartNewGame(typeA, typeB, botMatchStartFens[fenIndex], botAPlaysWhite);
            ++botMatchGameIndex;

            // UI Setup
            if (thread == currentThread)
            {
                boardUI.UpdatePosition(currentThread.board);
                boardUI.ResetSquareColours();
                SetBoardPerspective();
            }
        }

        public void StartNewMatch(PlayerType typeA, PlayerType typeB)
        {
            playerAType = typeA;
            playerBType = typeB;
            bool isGameWithHuman = typeA is PlayerType.Human || typeB is PlayerType.Human;

            botMatchGameIndex = 0;
            gamesPlayedThisMatch = 0;
            currentThread = gameThreads[0];

            if (isGameWithHuman)
            {
                StartNewGame(currentThread);
                for (int i = 1; i < gameThreads.Length; ++i) gameThreads[i].EndGame(GameResult.DrawByArbiter, log: false, autoStartNextBotMatch: false);
                HumanWasWhiteLastGame = !HumanWasWhiteLastGame;
            }
            else
            {
                foreach (GameThread thread in gameThreads) StartNewGame(thread);

                string nameA = GetPlayerName(typeA);
                string nameB = GetPlayerName(typeB);
                if (nameA == nameB)
                {
                    nameA += " (A)";
                    nameB += " (B)";
                }
                BotStatsA = new BotMatchStats(nameA);
                BotStatsB = new BotMatchStats(nameB);

                Log($"Starting new match: {nameA} vs {nameB}", false, ConsoleColor.Blue);
            }
        }

        public void OnMovePlayed(GameThread thread, Move move, bool animate)
        {
            if (thread == currentThread)
            {
                boardUI.UpdatePosition(currentThread.board, move, animate);
            }
        }

        public void OnGameEnded(GameThread thread, ChessPlayer playerWhite, ChessPlayer playerBlack, GameResult result, string pgn, bool autoStartNextBotMatch, bool log)
        {

            pgns.AppendLine(pgn);
            if (result != GameResult.DrawByArbiter)
            {
                totalMovesPlayed += thread.board.AllGameMoves.Count;
                ++totalGamesPlayed;
                ++gamesPlayedThisMatch;
            }

            if (log)
            {
                Log("Game Over: " + result + " Match: " + gamesPlayedThisMatch, false, ConsoleColor.Blue);
            }

            // If 2 bots playing each other, start next game automatically.
            if (playerWhite.IsBot && playerBlack.IsBot)
            {
                int numGamesToPlay = botMatchStartFens.Length * 2;

                UpdateBotMatchStats(result, thread.botAPlaysWhite);

                if (botMatchGameIndex < numGamesToPlay && autoStartNextBotMatch)
                {
                    if (fastForward)
                    {
                        StartNewGame(thread);
                    }
                    else
                    {
                        const int startNextGameDelayMs = 600;
                        System.Timers.Timer autoNextTimer = new(startNextGameDelayMs);
                        int originalGameID = thread.gameID;
                        autoNextTimer.Elapsed += (s, e) => {
                            if (originalGameID == thread.gameID) StartNewGame(thread);

                            autoNextTimer.Close();
                        };
                        autoNextTimer.AutoReset = false;
                        autoNextTimer.Start();
                    }
                }
                else if (autoStartNextBotMatch)
                {
                    fastForward = false;
                    Log("Match finished", false, ConsoleColor.Blue);
                }
            }
        }

        void SetBoardPerspective()
        {
            // Board perspective
            if (currentThread.PlayerWhite.IsHuman || currentThread.PlayerBlack.IsHuman)
            {
                boardUI.SetPerspective(currentThread.PlayerWhite.IsHuman);
            }
            else if (currentThread.PlayerWhite.Bot is MyBot && currentThread.PlayerBlack.Bot is MyBot)
            {
                boardUI.SetPerspective(true);
            }
            else
            {
                boardUI.SetPerspective(currentThread.PlayerWhite.Bot is MyBot);
            }
        }

        static int GetTokenCount()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "src", "My Bot", "MyBot.cs");

            using StreamReader reader = new(path);
            string txt = reader.ReadToEnd();
            return TokenCounter.CountTokens(txt);
        }


        void UpdateBotMatchStats(GameResult result, bool botAPlaysWhite)
        {
            UpdateStats(BotStatsA, botAPlaysWhite);
            UpdateStats(BotStatsB, !botAPlaysWhite);

            void UpdateStats(BotMatchStats stats, bool isWhiteStats)
            {
                // Draw
                if (Arbiter.IsDrawResult(result))
                {
                    stats.NumDraws++;
                }
                // Win
                else if (Arbiter.IsWhiteWinsResult(result) == isWhiteStats)
                {
                    stats.NumWins++;
                }
                // Loss
                else
                {
                    stats.NumLosses++;
                    stats.NumTimeouts += (result is GameResult.WhiteTimeout or GameResult.BlackTimeout) ? 1 : 0;
                    stats.NumIllegalMoves += (result is GameResult.WhiteIllegalMove or GameResult.BlackIllegalMove) ? 1 : 0;
                }
            }
        }

        public void Update()
        {
            foreach (GameThread thread in gameThreads) thread.Update();
        }

        public void Draw()
        {
            boardUI.Draw();
            string nameW = GetPlayerName(currentThread.PlayerWhite);
            string nameB = GetPlayerName(currentThread.PlayerBlack);
            boardUI.DrawPlayerNames(nameW, nameB, currentThread.PlayerWhite.TimeRemainingMs, currentThread.PlayerBlack.TimeRemainingMs, currentThread.isPlaying);
        }
        public void DrawOverlay()
        {
            BotBrainCapacityUI.Draw(tokenCount, MaxTokenCount);
            MenuUI.DrawButtons(this);
            MatchStatsUI.DrawMatchStats(this);
        }

        static string GetPlayerName(ChessPlayer player) => GetPlayerName(player.PlayerType);
        static string GetPlayerName(PlayerType type) => type.ToString();

        public int TotalGameCount => botMatchStartFens.Length * 2;
        public string AllPGNs => pgns.ToString();

        public class BotMatchStats
        {
            public string BotName;
            public int NumWins;
            public int NumLosses;
            public int NumDraws;
            public int NumTimeouts;
            public int NumIllegalMoves;

            public BotMatchStats(string name) => BotName = name;
        }

        public void Release()
        {
            boardUI.Release();
        }
    }
}
