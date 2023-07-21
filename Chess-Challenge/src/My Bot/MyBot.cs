using ChessChallenge.API;
using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;


public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    public int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    public int inf = 1000000000;
    public Move Think(Board board, Timer timer)
    {
        ////Console.WriteLine("################# Think ################");
        //Move bestMove = getBestMove(board, 3);
        ////Console.WriteLine(Evaluate(board));
        //return bestMove;

        Move[] moves = board.GetLegalMoves();

        Console.WriteLine(moves[0].ToString());

        board.MakeMove(moves[0]);
        Console.WriteLine(board.GetPiece(moves[0].TargetSquare));

        var pieceLists = board.GetAllPieceLists();
        foreach (PieceList list in pieceLists)
        {
            foreach (Piece piece in list)
            {
                Console.WriteLine(piece.ToString());
            }
        }

        board.UndoMove(moves[0]);

        return moves[0];
    }

    public int Evaluate(Board board)
    {
        int evaluation = 0;

        var pieceLists = board.GetAllPieceLists();
        foreach (PieceList list in pieceLists)
        {
            foreach (Piece piece in list)
            {
                evaluation += (pieceValues[(int)piece.PieceType] * (piece.IsWhite ? 1 : -1));
            }
        }

        if (board.IsInCheck()) evaluation += board.IsWhiteToMove ? -50 : 50;

        return evaluation;
    }

    public Move getBestMove(Board board, int depth)
    {
        int bestScore = board.IsWhiteToMove ? -inf : inf;
        Move bestMove = Move.NullMove;

        Move[] moves = board.GetLegalMoves();
        if (moves.Length == 0) Console.WriteLine("No moves");

        if (board.IsWhiteToMove)
        {
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int score = Minimax(board, depth - 1, -inf, inf);
                board.UndoMove(move);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }
        }
        else
        {
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                //int score = Minimax(board, depth - 1, -inf, inf);
                int score = Evaluate(board);
                board.UndoMove(move);
                //Console.WriteLine(move.StartSquare.ToString());
                //Console.WriteLine(move.TargetSquare.ToString());
                //Console.WriteLine(score);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }
        }
        //Console.WriteLine("BestScore: " + bestScore);

        return bestMove;
    }

    public int Minimax(Board board, int depth, int alpha, int beta)
    {
        if (board.IsInCheckmate()) return board.IsWhiteToMove ? -inf : inf;
        if (board.IsDraw()) return 0;

        if (depth <= 0) return Evaluate(board);
        //int bestScore = board.IsWhiteToMove ? -inf : inf;

        Move[] moves = board.GetLegalMoves();
        if (moves.Length == 0) Console.WriteLine("No moves");

        if (board.IsWhiteToMove)
        {
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                alpha = Math.Max(alpha, Minimax(board, depth - 1, alpha, beta));
                board.UndoMove(move);
                if (alpha >= beta) break;
            }
            return alpha;
        }
        else
        {
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                beta = Math.Min(beta, Minimax(board, depth - 1, alpha, beta));
                board.UndoMove(move);

                if (alpha >= beta) break;
            }
            return beta;
        }
    }
}