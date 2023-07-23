using ChessChallenge.API;
using System;

public class Entry
{
    static public int Exact = 0;
    static public int Alpha = 1;
    static public int Beta = 2;

    public ulong key;
    public int depth;
    public int value;
    public Move move;
    public int type;

    public Entry() {}
    public Entry (ulong key_p, int depth_p, int value_p, Move move_p, int type_p)
    {
        key = key_p;
        depth = depth_p;
        value = value_p;
        move = move_p;
        type = type_p;
    }
}

public class TranspositionTable
{
    static public int lookupFailed;
    public ulong size = 1_000_000;
    public Entry[] entries;
    public Board board;

    public TranspositionTable()
    {
        entries = new Entry[size];
    }


    public void Store(int depth, int value, Move move, int type)
    {
        //if (entries[board.ZobristKey % size].depth >= depth) return;

        entries[board.ZobristKey % size] = new Entry(board.ZobristKey, depth, value, move, type);
    }

    public int Lookup(int depth, int alpha, int beta)
    {
        Entry entry = entries[board.ZobristKey % size];

        if (entry == null ||
            entry.key != board.ZobristKey ||
            entry.depth < depth) return lookupFailed;

        if ((entry.type == Entry.Exact) ||
            (entry.type == Entry.Alpha && entry.value <= alpha) ||
            (entry.type == Entry.Beta && entry.value >= beta)) return entry.value;

        return lookupFailed;
    }

    public Move LookupMove() => entries[board.ZobristKey % size]?.move ?? Move.NullMove;
}

public class MyBot : IChessBot
{
    // Piece values:             null, pawn, knight, bishop, rook, queen, king
    public int[] pieceValues = { 0,    100,  300,    320,    500,  900,   10000 };
    public const int inf = 1000000000;
    public bool useTranspositionTable = true;

    public int positionsEvaluated;
    public int cutoffs;
    public int positionsLookedUp;

    public TranspositionTable TT = new();
    int[] scores = new int[200];

    public Board board;
    public Timer timer;
    public int timeToPlay;
    public int timeToMove;

    public Move bestMove;

    public Move Think(Board board_param, Timer timer_param)
    {
        positionsEvaluated = cutoffs = positionsLookedUp = 0;
        board = board_param;
        timer = timer_param;
        TT.board = board;

        if (timeToPlay == 0) timeToPlay = timer.MillisecondsRemaining;
        timeToMove = timer.MillisecondsRemaining / Math.Max(10, 40 - board.PlyCount) * 9 / 10 / 10;

        Console.WriteLine(timeToMove * (0.001));
        Move currentBestMove;
        int currentDepth = 1;
        bestMove = Move.NullMove;

        for (;;)
        {
            Search(currentDepth++, 0, -inf, inf);
            currentBestMove = bestMove;
            if (timer.MillisecondsElapsedThisTurn > timeToMove) break;
        }
        //{
        //    Search(6, 0, -inf, inf);
        //    currentBestMove = bestMove;
        //}

        //Console.WriteLine(bestMove.ToString());
        //Console.WriteLine("Time: " + timer.MillisecondsElapsedThisTurn +
        //                    " Cutoffs: " + cutoffs +
        //                    " Positions: " + positionsEvaluated +
        //                    " PositionsLookedUp " + positionsLookedUp +
        //                    " Depth: " + currentDepth);


        return currentBestMove;
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
        Move probablyBestMove = TT.LookupMove();
        int len = moves.Length;

        for (int i = 0; i < len; ++i)
        {
            scores[i] = 0;
            Move move = moves[i];
            if (move.IsCapture) scores[i] += pieceValues[(int)move.CapturePieceType] * 10 - pieceValues[(int)move.MovePieceType];

            if (move == probablyBestMove) scores[i] += 1000000;
        }

        // Sort
        for (int j = 0; j < len; ++j)
            for (int i = 1; i < len; ++i)
                if (scores[i - 1] < scores[i])
                    (scores[i - 1], scores[i], moves[i - 1], moves[i]) = (scores[i], scores[i - 1], moves[i], moves[i - 1]);
                //{
                    //(scores[i - 1], scores[i]) = (scores[i], scores[i - 1]);
                    //(moves[i - 1], moves[i]) = (moves[i], moves[i - 1]);
                //}
    }

    public int Search(int depth, int ply, int alpha, int beta)
    {
        if (timer.MillisecondsElapsedThisTurn > timeToMove) return 0;
        if (board.IsInCheckmate()) return -inf;
        if (board.IsDraw()) return -10;

        int lookup;

        if (useTranspositionTable && ply > 0 &&
            (lookup = TT.Lookup(depth, alpha, beta)) != TranspositionTable.lookupFailed)
        {
            ++positionsLookedUp;
            return lookup;
        }

        if (depth <= 0) return QuiescenceSearch(alpha, beta);

        int type = Entry.Alpha;
        Move[] moves = board.GetLegalMoves();
        Move currentBestMove = moves[0];
        OrderMoves(moves);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = -Search(depth - 1, ply + 1, -beta, -alpha);
            board.UndoMove(move);
            if (timer.MillisecondsElapsedThisTurn > timeToMove) return 0;
            if (eval >= beta)
            {
                if (useTranspositionTable) TT.Store(depth, eval, move, Entry.Beta);
                ++cutoffs;
                return beta;
            }
            if (eval > alpha)
            {
                type = Entry.Exact;
                currentBestMove = move;
                alpha = eval;
            }
        }

        if (useTranspositionTable) TT.Store(depth, alpha, currentBestMove, type);

        if (ply == 0) bestMove = currentBestMove;
        return alpha;
    }

    public int QuiescenceSearch(int alpha, int beta)
    {
        if (timer.MillisecondsElapsedThisTurn > timeToMove) return 0;
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
}