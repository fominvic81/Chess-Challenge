using ChessChallenge.API;
using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;


public class Entry
{
    public int depth;
    public int move;

    public Entry()
    {
        depth = -1;
    }
    public Entry (int depth, int move)
    {
        this.depth = depth;
        this.move = move;
    }
}

public class MyBot : IChessBot
{
    // Piece values:             null, pawn, knight, bishop, rook, queen, king
    public int[] pieceValues = { 0,    100,  300,    320,    500,  900,   10000 };
    public int inf = 1000000000;
    public bool amIWhite;

    public int positionsEvaluated;
    public int cutoffs;

    public Board board;

    public Move Think(Board board, Timer timer)
    {
        this.board = board;

        amIWhite = board.IsWhiteToMove;
        Move bestMove = Search(6);
        Console.WriteLine("############ Info ###############");
        Console.WriteLine("Time: " + timer.MillisecondsElapsedThisTurn + " Cutoffs: " + cutoffs + " Positions: " + positionsEvaluated);

        return bestMove;
    }

    public int Evaluate()
    {
        ++positionsEvaluated;
        int evaluation = 0;

        var pieceLists = board.GetAllPieceLists();
        foreach (PieceList list in pieceLists)
            foreach (Piece piece in list) evaluation += (pieceValues[(int)piece.PieceType] * (piece.IsWhite ? 1 : -1));

        //if (board.IsInCheck()) evaluation += board.IsWhiteToMove ? -50 : 50;

        if (!board.IsWhiteToMove) evaluation = -evaluation;
        if (board.IsInCheck()) evaluation += -50;
        return evaluation;
    }
    public void OrderMoves(Move[] moves)
    {
        int[] scores = new int[moves.Length];

        for (int i = 0; i < moves.Length; ++i)
        {
            scores[i] = 0;
            Move move = moves[i];
            if (move.IsCapture)
            {
                 scores[i] += pieceValues[(int)move.CapturePieceType] * 10 - pieceValues[(int)move.MovePieceType];
            }
            else if (move.MovePieceType != PieceType.Pawn && board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                scores[i] -= 1000;
            }
        }

        // Sort
        for (int j = 0; j < moves.Length; ++j)
            for (int i = 1; i < moves.Length; ++i)
                if (scores[i - 1] < scores[i])
                    (scores[i - 1], scores[i], moves[i - 1], moves[i]) = (scores[i], scores[i - 1], moves[i], moves[i - 1]);
                //{
                    //(scores[i - 1], scores[i]) = (scores[i], scores[i - 1]);
                    //(moves[i - 1], moves[i]) = (moves[i], moves[i - 1]);
                //}
    }
    public Move Search(int depth)
    {
        positionsEvaluated = cutoffs = 0;

        int bestScore = -inf;
        Move bestMove = Move.NullMove;

        Move[] moves = board.GetLegalMoves();


        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int score = -Minimax(depth - 1, -inf, inf);
            board.UndoMove(move);
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    public int Minimax(int depth, int alpha, int beta)
    {
        if (board.IsInCheckmate()) return -inf;
        if (board.IsDraw()) return -10;

        if (depth <= 0) return QuiescenceSearch(board, alpha, beta);

        Move[] moves = board.GetLegalMoves();
        OrderMoves(moves);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = -Minimax(depth - 1, -beta, -alpha);
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

    public int QuiescenceSearch(Board board, int alpha, int beta)
    {
        if (board.IsInCheckmate()) return -inf;
        if (board.IsDraw()) return -10;

        int eval = Evaluate();
        if (eval >= beta)
        {
            ++cutoffs;
            return beta;
        }
        if (eval > alpha) alpha = eval;

        Move[] moves = board.GetLegalMoves(capturesOnly: true);
        if (moves.Length == 0) return Evaluate();

        OrderMoves(moves);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            eval = -QuiescenceSearch(board, -beta, -alpha);
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