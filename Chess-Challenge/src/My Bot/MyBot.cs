//#define Stats
using ChessChallenge.API;
using System;
using System.Linq;

public class MyBot : IChessBot
{

    public int[,,] history = new int[2, 64, 64];

    public int[]
        pieceValues =
            { 0, 82, 337, 365, 477, 1025, 0, 94, 281, 297, 512, 936, 0 },
        reverseScores = new int[200],
        phaseWeights = { 0, 1, 1, 2, 4, 0 },
        pieceSquareTables,
        killerMoveA = new int[256],
        killerMoveB = new int[256];

    public int
        inf = 1000000000,
        timeToMove;

    public record struct Entry(ulong Key, int Depth, int Value, Move Move, int Type);

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

    static public readonly Entry[] entries = new Entry[2097152]; // 2 << 20
    public ulong TTIndex => board.ZobristKey & 2097151; // (2 << 20) - 1

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

    public MyBot()
    {
        //Console.WriteLine(System.Runtime.InteropServices.Marshal.SizeOf<Entry>() * entries.Length / 1024 / 1024); // Transposition table size

        pieceSquareTables = pieceSquareTablesCompressed
            .SelectMany(x => decimal.GetBits(x).Take(3))
            .SelectMany(BitConverter.GetBytes)
            .Select(x => x * 375 / 255 - 176)
            .ToArray();

        for (int i = 0; i < 768;)
            pieceSquareTables[i] += pieceValues[i++ / 64 + 1];
    }

    public Move Think(Board board_param, Timer timer_param)
    {
#if Stats
        positionsEvaluated = cutoffs = positionsLookedUp = 0;
#endif
        board = board_param;
        timer = timer_param;


        int currentDepth = 0;
        bestMove = board.GetLegalMoves()[0];

        timeToMove = Math.Max(150, timer.MillisecondsRemaining - 1000) * 4 / 5 / Math.Max(20, 60 - board.PlyCount);  // TODO: increment

        while (!endSearch) Search(0, ++currentDepth, -inf, inf);

        //timeToMove = 100000;
        //for (; currentDepth < 6;) Search(0, ++currentDepth, -inf, inf);

#if Stats
        Console.WriteLine("Time: " + timer.MillisecondsElapsedThisTurn +
                            " " + bestMove.ToString() +
                            " Cutoffs: " + cutoffs +
                            " Positions: " + positionsEvaluated +
                            " PositionsLookedUp " + positionsLookedUp +
                            " Depth: " + currentDepth);
#endif

        return bestMove;
    }

    public int Evaluate()
    {
#if Stats
        ++positionsEvaluated;
#endif

        int middlegame = 0, endgame = 0,
            phase = 0;

        foreach (bool white in stackalloc[] { true, false })
        {
            //var enemyKing = board.GetKingSquare(!white);
            // Material
            for (int piece = 0; piece < 6; ++piece)
            {
                ulong bitboard = board.GetPieceBitboard((PieceType)(piece + 1), white);

                while (bitboard != 0)
                {
                    int square = BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard),
                        index = piece * 64 + square ^ (white ? 0 : 56);
                    middlegame += pieceSquareTables[index];
                    endgame += pieceSquareTables[index + 384];
                    phase += phaseWeights[piece];

                    // king safety
                    //if (piece != 5) endgame += (Math.Abs((square & 7) - enemyKing.File) + Math.Abs((square >> 3) - enemyKing.Rank)) * pieceValues[piece + 1] / 14 / 40;
                }
            }

            //King shield
            //middleGame += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetKingAttacks(board.GetKingSquare(white)) & board.GetPieceBitboard(PieceType.Pawn, white)) * 10;

            endgame = -endgame;
            middlegame = -middlegame;
        }

        //foreach (bool white in stackalloc[] { true, false })
        //{
        //    if (piecesNum < 4 && endgame * (white ? 1 : -1) > 300)
        //    {
        //        Square king = board.GetKingSquare(white), enemyKing = board.GetKingSquare(!white);
        //        int centerManhattanDistance = (enemyKing.File ^ ((enemyKing.File) - 4) >> 8) + (enemyKing.Rank ^ ((enemyKing.Rank) - 4) >> 8);
        //        int kingManhattanDistance = Math.Abs(king.File - enemyKing.File) + Math.Abs(king.Rank - enemyKing.Rank);
        //        int mopupEval = (47 * centerManhattanDistance + 16 * (14 - kingManhattanDistance)) / 10;
        //        endgame += mopupEval;
        //    }
        //    endgame = -endgame;
        //}
        if (phase > 24) phase = 24;
        return (middlegame * phase + endgame * (24 - phase)) / (board.IsWhiteToMove ? 24 : -24);
    }
    public int Search(int plyFromRoot, int plyRemaining, int alpha, int beta)
    {
        if (endSearch || board.IsInsufficientMaterial() || board.IsRepeatedPosition() || board.IsFiftyMoveDraw()) return 0;

        bool isInCheck = board.IsInCheck();
        if (isInCheck) ++plyRemaining;

        // Lookup value from transposition table
        Entry entry = entries[TTIndex];
        int type = entry.Type,
            value = entry.Value,
            eval = 0,
            turn = board.IsWhiteToMove ? 1 : 0;

        if (plyFromRoot > 0 &&
            entry.Key == board.ZobristKey &&
            entry.Depth >= plyRemaining && (
            (type == Exact) ||
            (type == Alpha && value <= alpha) ||
            (type == Beta && value >= beta)
            ))
#if Stats
            { ++positionsLookedUp; return value; }
#else
            return value;
#endif
        type = Alpha;

        if (plyRemaining <= 0)
        {
            eval = Evaluate();
            if (eval >= beta)
#if Stats
            { ++cutoffs; return beta; }
#else
                return beta;
#endif
            if (eval > alpha) alpha = eval;
        }

        ////eval = Evaluate();
        ////if (!isInCheck && eval - 85 * plyRemaining >= beta) return eval - 85 * plyRemaining;
        //if (plyRemaining >= 2 && plyFromRoot > 0 && !isInCheck)
        //{
        //    board.ForceSkipTurn();

        //    eval = -Search(plyFromRoot + 1, plyRemaining - 3 - plyRemaining / 6, -beta, 1 - beta);

        //    board.UndoSkipTurn();

        //    // TODO: add stats
        //    if (eval >= beta) return eval;
        //}

        var moves = board.GetLegalMoves(plyRemaining <= 0);
        if (moves.Length == 0) return isInCheck ? -10000000 + plyFromRoot : eval;

        // Move ordering
        for (int i = 0; i < moves.Length; ++i)
        {
            Move move = moves[i];
            reverseScores[i] =
                -(move == entries[TTIndex].Move ? 10000000 : // hashed move
                move.IsCapture ? pieceValues[(int)move.CapturePieceType + 6] * (board.SquareIsAttackedByOpponent(move.TargetSquare) ? 100 : 1000) - pieceValues[(int)move.MovePieceType + 6] : // captures
                move.RawValue == killerMoveA[plyFromRoot] || move.RawValue == killerMoveB[plyFromRoot] ? 8000 : // killer moves
                history[turn, move.StartSquare.Index, move.TargetSquare.Index]); // history
        }
        Array.Sort(reverseScores, moves, 0, moves.Length);

        Move currentBestMove = moves[0];

        for (int i = 0; i < moves.Length; ++i)
        {
            Move move = moves[i];
            bool needsFullSearch = true;
            board.MakeMove(move);

            if (i >= 3 && plyRemaining >= 3 && !move.IsCapture)
                needsFullSearch = (eval = -Search(plyFromRoot + 1, plyRemaining - 2, -alpha - 1, -alpha)) > alpha;

            if (needsFullSearch)
                eval = -Search(plyFromRoot + 1, plyRemaining - 1, -beta, -alpha);

            board.UndoMove(move);
            if (endSearch) return 0;
            if (eval >= beta)
            {
                // Store position in Transposition Table
                if (needsFullSearch)
                    entries[TTIndex] = new(board.ZobristKey, plyRemaining, eval, currentBestMove, Beta);

                if (!move.IsCapture)
                {
                    killerMoveB[plyFromRoot] = killerMoveA[plyFromRoot];
                    killerMoveA[plyFromRoot] = move.RawValue;
                    history[turn, move.StartSquare.Index, move.TargetSquare.Index] += plyRemaining; // plyRemaining * plyRemaining ??
                }
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
                if (plyFromRoot == 0) bestMove = currentBestMove;
            }
        }

        // Store position in Transposition Table
        entries[TTIndex] = new(board.ZobristKey, plyRemaining, alpha, currentBestMove, type);

        return alpha;
    }
}