using ChessChallenge.Chess;
using ChessChallenge.Example;
using Raylib_cs;
using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using static ChessChallenge.Application.Settings;
using static ChessChallenge.Application.ConsoleHelper;
using static ChessChallenge.Application.ChallengeController;

namespace ChessChallenge.Application
{

    public class GameThread
    {
        static int lastGameID = 0;
        public int gameID = lastGameID++;
        public bool isPlaying;
        public Board board;
        public ChessPlayer PlayerWhite { get; private set; }
        public ChessPlayer PlayerBlack { get; private set; }

        float lastMoveMadeTime;
        bool isWaitingToPlayMove;
        Move moveToPlay;
        float playMoveTime;
        public bool botAPlaysWhite;

        ChessPlayer PlayerToMove => board.IsWhiteToMove ? PlayerWhite : PlayerBlack;
        ChessPlayer PlayerNotOnMove => board.IsWhiteToMove ? PlayerBlack : PlayerWhite;

        AutoResetEvent botTaskWaitHandle;
        bool hasBotTaskException;
        ExceptionDispatchInfo botExInfo;

        readonly MoveGenerator moveGenerator;
        readonly ChallengeController controller;

        public GameThread(ChallengeController controller)
        {
            this.controller = controller;

            moveGenerator = new();
            board = new Board();

            botTaskWaitHandle = new AutoResetEvent(false);
        }

        public void StartNewGame(PlayerType whiteType, PlayerType blackType, string fen, bool botAPlaysWhite = true)
        {
            // End ongoing game
            EndGame(GameResult.DrawByArbiter, log: false, autoStartNextBotMatch: false);

            // Create new task
            botTaskWaitHandle = new AutoResetEvent(false);
            Task.Factory.StartNew(BotThinkerThread, TaskCreationOptions.LongRunning);

            // Board Setup
            board = new Board();
            board.LoadPosition(fen);

            // Player Setup
            PlayerWhite = CreatePlayer(whiteType);
            PlayerBlack = CreatePlayer(blackType);
            PlayerWhite.SubscribeToMoveChosenEventIfHuman(OnMoveChosen);
            PlayerBlack.SubscribeToMoveChosenEventIfHuman(OnMoveChosen);

            // Start
            this.botAPlaysWhite = botAPlaysWhite;
            isPlaying = true;
            NotifyTurnToMove();
        }

        public void Update()
        {

            if (isPlaying)
            {
                PlayerWhite.Update();
                PlayerBlack.Update();

                PlayerToMove.UpdateClock(Raylib.GetFrameTime());
                if (PlayerToMove.TimeRemainingMs <= 0)
                {
                    EndGame(PlayerToMove == PlayerWhite ? GameResult.WhiteTimeout : GameResult.BlackTimeout);
                }
                else
                {
                    if (isWaitingToPlayMove && (Raylib.GetTime() >= playMoveTime || controller.fastForward))
                    {
                        isWaitingToPlayMove = false;
                        PlayMove(moveToPlay);
                    }
                }
            }

            if (hasBotTaskException)
            {
                hasBotTaskException = false;
                botExInfo.Throw();
            }

        }

        public void EndGame(GameResult result, bool log = true, bool autoStartNextBotMatch = true)
        {
            gameID = lastGameID++;

            // Allow task to terminate
            botTaskWaitHandle.Set();

            if (isPlaying)
            {
                isPlaying = false;
                isWaitingToPlayMove = false;

                string pgn = PGNCreator.CreatePGN(board, result, GetPlayerName(PlayerWhite), GetPlayerName(PlayerBlack));

                controller.OnGameEnded(this, PlayerWhite, PlayerBlack, result, pgn, autoStartNextBotMatch, log);
            }
        }

        void BotThinkerThread()
        {
            int threadID = gameID;
            //Console.WriteLine("Starting thread: " + threadID);

            while (true)
            {
                // Sleep thread until notified
                botTaskWaitHandle.WaitOne();
                // Get bot move
                if (threadID == gameID)
                {
                    var move = GetBotMove();

                    if (threadID == gameID)
                    {
                        OnMoveChosen(move);
                    }
                }
                // Terminate if no longer playing this game
                if (threadID != gameID)
                {
                    break;
                }
            }
            //Console.WriteLine("Exitting thread: " + threadID);
        }

        Move GetBotMove()
        {
            API.Board botBoard = new(board);

            try
            {
                API.Timer timer = new(PlayerToMove.TimeRemainingMs, PlayerNotOnMove.TimeRemainingMs, GameDurationMilliseconds);
                API.Move move = PlayerToMove.Bot.Think(botBoard, timer);
                return new Move(move.RawValue);
            }
            catch (Exception e)
            {
                Log("An error occurred while bot was thinking.\n" + e.ToString(), true, ConsoleColor.Red);
                hasBotTaskException = true;
                botExInfo = ExceptionDispatchInfo.Capture(e);
            }
            return Move.NullMove;
        }

        void PlayMove(Move move)
        {
            if (isPlaying)
            {
                bool animate = PlayerToMove.IsBot;
                lastMoveMadeTime = (float)Raylib.GetTime();

                board.MakeMove(move, false);

                GameResult result = Arbiter.GetGameState(board);
                if (result == GameResult.InProgress)
                {
                    NotifyTurnToMove();
                }
                else
                {
                    EndGame(result);
                }
                controller.OnMovePlayed(this, move, animate);
            }
        }

        void NotifyTurnToMove()
        {
            //playerToMove.NotifyTurnToMove(board);
            if (PlayerToMove.IsHuman)
            {
                PlayerToMove.Human.SetPosition(FenUtility.CurrentFen(board));
                PlayerToMove.Human.NotifyTurnToMove();
            }
            else
            {
                botTaskWaitHandle.Set();
            }
        }

        void OnMoveChosen(Move chosenMove)
        {
            if (IsLegal(chosenMove))
            {
                if (PlayerToMove.IsBot)
                {
                    moveToPlay = chosenMove;
                    playMoveTime = lastMoveMadeTime + MinMoveDelay;
                    isWaitingToPlayMove = true;
                }
                else
                {
                    PlayMove(chosenMove);
                }
            }
            else
            {
                string moveName = MoveUtility.GetMoveNameUCI(chosenMove);
                string log = $"Illegal move: {moveName} in position: {FenUtility.CurrentFen(board)}";
                Log(log, true, ConsoleColor.Red);
                GameResult result = PlayerToMove == PlayerWhite ? GameResult.WhiteIllegalMove : GameResult.BlackIllegalMove;
                EndGame(result);
            }
        }
        bool IsLegal(Move givenMove)
        {
            var moves = moveGenerator.GenerateMoves(board);
            foreach (var legalMove in moves)
            {
                if (givenMove.Value == legalMove.Value)
                {
                    return true;
                }
            }

            return false;
        }

        static string GetPlayerName(ChessPlayer player) => GetPlayerName(player.PlayerType);
        static string GetPlayerName(PlayerType type) => type.ToString();

        ChessPlayer CreatePlayer(PlayerType type)
        {
            return type switch {
                PlayerType.MyBot => new ChessPlayer(new MyBot(), type, GameDurationMilliseconds),
                PlayerType.EvilBot => new ChessPlayer(new EvilBot(), type, GameDurationMilliseconds),
                PlayerType.PrevBot => new ChessPlayer(new PrevBot(), type, GameDurationMilliseconds),
                PlayerType.NegamaxV1 => new ChessPlayer(new NegamaxV1(), type, GameDurationMilliseconds),
                PlayerType.LiteBlueV5 => new ChessPlayer(new LiteBlueV5(), type, GameDurationMilliseconds),
                _ => new ChessPlayer(new HumanPlayer(controller.boardUI), type)
            };
        }
    }
}