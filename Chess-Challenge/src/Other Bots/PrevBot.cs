using ChessChallenge.API;
using System;
using System.Linq;

namespace ChessChallenge.Example
{

    public class Entry
    {
        public ulong key;
        public int depth, value, type;
        public Move move;

        public Entry() { }

        public Entry(ulong key_p, int depth_p, int value_p, Move move_p, int type_p)
        {
            (key, depth, value, move, type) = (key_p, depth_p, value_p, move_p, type_p);
        }
    }

    public class PrevBot : IChessBot
    {
        public int[]
            // null, pawn, knight, bishop, rook, queen, king
            pieceValues = { 0, 100, 300, 320, 500, 900, 10000 },
            reverseScores = new int[200],
            pieceSquareTables;

        public int
            inf = 1000000000,
            timeToMove;

#if Stats
    public int
        positionsEvaluated,
        cutoffs,
        positionsLookedUp;
#endif

        public Board board;
        public Timer timer;

        public Move bestMove;

        public bool endSearch => timer.MillisecondsElapsedThisTurn > timeToMove;

        static public int Exact = 0, Alpha = 1, Beta = 2, lookupFailed;

        static public ulong TTSize = 1_000_000;
        static public Entry[] entries = new Entry[TTSize];
        public ulong TTIndex => board.ZobristKey % TTSize;

        public decimal[] pieceSquareTablesCompressed = {
            32004456945391047372753631352m,
            34527499795583066095857404269m,
            41930994349317695410235799140m,
            32663516463863011804700770440m,
            37283841182613612605372422334m,
            32005576384313939411869399160m,
            39459874287317914087995691619m,
            35445126771900143322400457861m,
            51861271151822516935499023473m,
            33576273775586402387314398644m,
            32629466884660464117052553216m,
            37622411340409385605226327918m,
            43174979111738014384500474744m,
            36671039056380237072490791056m,
            34173326644721193841776366188m,
            35432833748355182153597035149m,
            35099254967069289411848728170m,
            29851365280217876910430191735m,
            43170062905136476261956150622m,
            40741520366248297589663502729m,
            48762733937932907951436238731m,
            26114490330755200817554161317m,
            36655204294954159417281507935m,
            36667274810680425651787430260m,
            39146739018415633910190597220m,
            49378128478343737561260400781m,
            25223719787636649843057588324m,
            39147947553465782484154803326m,
            28594077022346465301060152942m,
            29225056259763336636878837847m,
            35718277910649002016531646833m,
            40070296381126839255850317170m,
            39457518307069914828458457208m,
            35425703309040256900488853633m,
            38223257249457605776401399425m,
            55924448587093310875271199606m,
            37283841192944593318917633272m,
            14945215015295331115394955384m,
            39149142571013704740556403302m,
            33267827685660614960433754487m,
            40686957606134892769648538475m,
            27359698257285778281037657727m,
            36025316031479562592168924003m,
            31080809801441928223550368881m,
            41318035994942441946311718511m,
            37595777286111216795169226621m,
            34504506446325449338790638201m,
            32008055757151261616764186230m,
            37593302487858397545078880625m,
            33562724916804139389115462001m,
            37604216024876137265944755066m,
            36660026071086921194816371065m,
            40703925549604886605729988992m,
            27981048310794252786973180032m,
            38534984736129110853774632808m,
            42269565008351546207098602622m,
            48123088696497823225932777594m,
            37309314404851227146452308378m,
            34789708458190682178510686321m,
            33565199714264861584472370788m,
            42563205830030198110079186538m,
            37925894482056208982422227083m,
            41009807062269881895187154047m,
            33573699640302844113553232772m,
        };

        public PrevBot()
        {

            pieceSquareTables = pieceSquareTablesCompressed
                .SelectMany(x => decimal.GetBits(x).Take(3))
                .SelectMany(BitConverter.GetBytes)
                .Select(x => x * 375 / 255 - 176)
                .ToArray();

            for (int i = 0; i < 768; ++i)
                pieceSquareTables[i] += pieceValues[i / 64 % 6 + 1];
        }

        public Move Think(Board board_param, Timer timer_param)
        {
#if Stats
        positionsEvaluated = cutoffs = positionsLookedUp = 0;
#endif
            board = board_param;
            timer = timer_param;


            int currentDepth = 1;
            bestMove = board.GetLegalMoves()[0];

            timeToMove = Math.Max(150, timer.MillisecondsRemaining - 1000) * 4 / 5 / Math.Max(20, 60 - board.PlyCount);

            for (; !endSearch;) Search(currentDepth, currentDepth++, true, -inf, inf);

            //Search(0, 0, true, -inf, inf);

            //timeToMove = 100000;
            //for (; currentDepth <= 6;) Search(currentDepth, currentDepth++, true, -inf, inf);
            //Console.WriteLine(timer.MillisecondsElapsedThisTurn);

            //int eval = 0;
            //for (; !endSearch;)
            //{
            //    int currendEval = Search(currentDepth, currentDepth++, true, -inf, inf);
            //    if (!endSearch) eval = currendEval;
            //}
            //Console.WriteLine(eval);

#if Stats
        Console.WriteLine("Time: " + timer.MillisecondsElapsedThisTurn +
                            " " + bestMove.ToString() +
                            " Cutoffs: " + cutoffs +
                            " Positions: " + positionsEvaluated +
                            " PositionsLookedUp " + positionsLookedUp +
                            " Depth: " + (currentDepth - 1));
#endif

            return bestMove;
        }

        public int Evaluate()
        {
#if Stats
        ++positionsEvaluated;
#endif

            int middleGame = 0, endgame = 0,
                piecesNum = BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard);

            foreach (PieceList list in board.GetAllPieceLists())
                foreach (Piece piece in list)
                {
                    int index = ((int)piece.PieceType - 1) * 64 + piece.Square.Index ^ (piece.IsWhite ? 0 : 56), perspective = (piece.IsWhite ? 1 : -1);
                    middleGame += pieceSquareTables[index] * perspective;
                    endgame += pieceSquareTables[index + 384] * perspective;
                }

            return (middleGame * piecesNum + endgame * (32 - piecesNum)) / (board.IsWhiteToMove ? 32 : -32);
        }

        public int Search(int depth, int plyRemaining, bool isRoot, int alpha, int beta)
        {
            // 50??
            if (endSearch || board.IsInsufficientMaterial() || board.IsRepeatedPosition() || board.FiftyMoveCounter >= 100) return 0;

            // Lookup value from transposition table
            Entry entry = entries[TTIndex];
            if (!isRoot &&
                entry != null &&
                entry.key == board.ZobristKey &&
                entry.depth >= plyRemaining && (
                (entry.type == Exact) ||
                (entry.type == Alpha && entry.value <= alpha) ||
                (entry.type == Beta && entry.value >= beta)
                ))
#if Stats
            { ++positionsLookedUp; return entry.value; }
#else
                return entry.value;
#endif

            Move[] moves = board.GetLegalMoves(plyRemaining <= 0);

            int eval;
            if (plyRemaining <= 0)
            {
                eval = Evaluate();
                if (moves.Length == 0) return eval;
                if (eval >= beta)
#if Stats
            { ++cutoffs; return beta; }
#else
                    return beta;
#endif
                if (eval > alpha) alpha = eval;
            }

            if (moves.Length == 0) return board.IsInCheck() ? -10000000 - depth : 0;


            // Move ordering
            Move? probablyBestMove = entries[TTIndex]?.move;

            for (int i = 0; i < moves.Length; ++i)
            {
                reverseScores[i] = 0;
                Move move = moves[i];
                if (move.IsCapture) reverseScores[i] -= pieceValues[(int)move.CapturePieceType] * 10 - pieceValues[(int)move.MovePieceType];
                if ((BitboardHelper.GetPawnAttacks(move.TargetSquare, board.IsWhiteToMove) & board.GetPieceBitboard(PieceType.Pawn, !board.IsWhiteToMove)) != 0) reverseScores[i] += 100;
                if (move == probablyBestMove) reverseScores[i] -= 1000000;
            }

            Array.Sort(reverseScores, moves, 0, moves.Length);

            Move currentBestMove = moves[0];
            int type = Alpha;

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                eval = -Search(depth - 1, plyRemaining - 1 + (board.IsInCheck() ? 1 : 0), false, -beta, -alpha);
                board.UndoMove(move);
                if (endSearch) return 0;
                if (eval >= beta)
                {
                    // Store position in Transposition Table
                    entries[TTIndex] = new(board.ZobristKey, plyRemaining, eval, currentBestMove, Beta);
#if Stats
                ++cutoffs;
#endif
                    return beta;
                }
                if (eval > alpha)
                {
                    type = Exact;
                    currentBestMove = move;
                    alpha = eval;
                }
            }

            // Store position in Transposition Table
            entries[TTIndex] = new(board.ZobristKey, plyRemaining, alpha, currentBestMove, type);

            if (isRoot) bestMove = currentBestMove;
            return alpha;
        }
    }
}