using ChessChallenge.API;
using System;
using System.Runtime.InteropServices;

public class Entry
{
    public ulong key;
    public int depth;
    public int value;
    public Move move;
    public int type;

    public Entry() { }
    public Entry(ulong key_p, int depth_p, int value_p, Move move_p, int type_p)
    {
        key = key_p;
        depth = depth_p;
        value = value_p;
        move = move_p;
        type = type_p;
    }
}

public class MyBot : IChessBot
{
    // Piece values:             null, pawn, knight, bishop, rook, queen, king
    public int[] pieceValues = { 0, 100, 300, 320, 500, 900, 10000 };
    public const int inf = 1000000000;
    public bool useTranspositionTable = true;

    public int positionsEvaluated;
    public int cutoffs;
    public int positionsLookedUp;

    public int[] scores = new int[200];

    public Board board;
    public Timer timer;
    public int timeToMove;

    public Move bestMove;

    public bool endSearch
    {
        get => timer.MillisecondsElapsedThisTurn > timeToMove;
    }

    static public int Exact = 0;
    static public int Alpha = 1;
    static public int Beta = 2;

    static public int lookupFailed;
    public ulong TTSize = 1_000_000;
    public Entry[] entries;

    public MyBot()
    {
        entries = new Entry[TTSize];
    }

    public Move Think(Board board_param, Timer timer_param)
    {
        positionsEvaluated = cutoffs = positionsLookedUp = 0;
        board = board_param;
        timer = timer_param;

        timeToMove = Math.Max(200, timer.MillisecondsRemaining - 2000) * 4 / 5 / Math.Max(20, 60 - board.PlyCount);

        int currentDepth = 1;
        bestMove = board.GetLegalMoves()[0];
        for (; !endSearch;) Search(currentDepth++, 0, -inf, inf);

        //Console.WriteLine("Time: " + timer.MillisecondsElapsedThisTurn +
        //                    " " + bestMove.ToString() +
        //                    " Cutoffs: " + cutoffs +
        //                    " Positions: " + positionsEvaluated +
        //                    " PositionsLookedUp " + positionsLookedUp +
        //                    " Depth: " + (currentDepth - 1));

        return bestMove;
    }

    public int Evaluate()
    {
        ++positionsEvaluated;
        int evaluation = 0;

        foreach (PieceList list in board.GetAllPieceLists())
            foreach (Piece piece in list) evaluation += pieceValues[(int)piece.PieceType] * (piece.IsWhite ? 1 : -1);

        if (!board.IsWhiteToMove) evaluation = -evaluation;
        if (board.IsInCheck()) evaluation += -50;

        return evaluation;
    }
    public void OrderMoves(Move[] moves)
    {
        Move probablyBestMove = LookupMove();
        int len = moves.Length;

        for (int i = 0; i < len; ++i)
        {
            scores[i] = 0;
            Move move = moves[i];
            if (move.IsCapture) scores[i] += pieceValues[(int)move.CapturePieceType] * 10 - pieceValues[(int)move.MovePieceType];

            if (move == probablyBestMove) scores[i] += 1000000;
        }

        // Sort. Lol
        for (int i = 1; i < len; ++i)
            if (scores[i - 1] < scores[i])
                (i, scores[i - 1], scores[i], moves[i - 1], moves[i]) = (1, scores[i], scores[i - 1], moves[i], moves[i - 1]);
    }

    public int Search(int depth, int ply, int alpha, int beta)
    {
        if (endSearch) return 0;
        if (board.IsInCheckmate()) return -inf;
        if (board.IsDraw()) return -10;

        int lookup;

        if (useTranspositionTable && ply > 0 &&
            (lookup = Lookup(depth, alpha, beta)) != lookupFailed)
        {
            ++positionsLookedUp;
            return lookup;
        }

        if (depth <= 0) return QuiescenceSearch(alpha, beta);

        int type = Alpha;
        Move[] moves = board.GetLegalMoves();

        Move currentBestMove = moves[0];
        OrderMoves(moves);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = -Search(depth - 1, ply + 1, -beta, -alpha);
            board.UndoMove(move);
            if (endSearch) return 0;
            if (eval >= beta)
            {
                if (useTranspositionTable) Store(depth, eval, move, Beta);
                ++cutoffs;
                return beta;
            }
            if (eval > alpha)
            {
                type = Exact;
                currentBestMove = move;
                alpha = eval;
            }
        }

        if (useTranspositionTable) Store(depth, alpha, currentBestMove, type);

        if (ply == 0) bestMove = currentBestMove;
        return alpha;
    }

    public int QuiescenceSearch(int alpha, int beta)
    {
        if (endSearch) return 0;
        if (board.IsInCheckmate()) return -inf;
        if (board.IsDraw()) return -10;

        int eval = Evaluate();
        if (eval >= beta)
        {
            ++cutoffs;
            return beta;
        }
        if (eval > alpha) alpha = eval;

        Move[] moves = board.GetLegalMoves(true);
        if (moves.Length == 0) return Evaluate();

        OrderMoves(moves);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            eval = -QuiescenceSearch(-beta, -alpha);
            board.UndoMove(move);
            if (eval >= beta)
            {
                ++cutoffs;
                return beta;
            }
            if (eval > alpha) alpha = eval;
        }
        return alpha;
    }


    // Transposition Table
    public ulong Index {
        get => board.ZobristKey % TTSize;
    }

    public void Store(int depth, int value, Move move, int type)
    {
        //if (entries[board.ZobristKey % size].depth >= depth) return;

        entries[Index] = new Entry(board.ZobristKey, depth, value, move, type);
    }

    public int Lookup(int depth, int alpha, int beta)
    {
        Entry entry = entries[Index];

        if (entry == null ||
            entry.key != board.ZobristKey ||
            entry.depth < depth) return lookupFailed;

        if ((entry.type == Exact) ||
            (entry.type == Alpha && entry.value <= alpha) ||
            (entry.type == Beta && entry.value >= beta)) return entry.value;

        return lookupFailed;
    }

    public Move LookupMove() => entries[Index]?.move ?? Move.NullMove;
}