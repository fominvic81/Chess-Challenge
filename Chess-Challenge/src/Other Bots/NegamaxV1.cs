using ChessChallenge.API;
using System;
using System.Linq;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class NegamaxV1 : IChessBot
    {
        public int[]
            // null, pawn, knight, bishop, rook, queen, king
            pieceValues = { 0, 100, 300, 320, 500, 900, 10000 },
            scores = new int[200],
            pieceSquareTables;

        public int
            inf = 1000000000,
            timeToMove,
            positionsEvaluated,
            cutoffs,
            positionsLookedUp;

        public bool useTranspositionTable = true;

        public Board board;
        public Timer timer;

        public Move bestMove;

        public bool endSearch
        {
            get => timer.MillisecondsElapsedThisTurn > timeToMove;
        }

        static public int Exact = 0, Alpha = 1, Beta = 2, lookupFailed;

        public ulong TTSize = 1_000_000;
        public Entry[] entries;

        public decimal[] pieceSquareTablesCompressed = {
            32004456945391047372753631352m,
            34527499795583066095857404269m,
            41930994349317695410235799140m,
            32663516463863011804700770440m,
            37283841182613612605372422334m,
            37283841183183217691079506040m,
            37587305230719129726845091457m,
            36976745598207640009765778552m,
            52213207395237709114402507151m,
            79208630268719441335417020064m,
            29805353825597328969742973048m,
            32939023159659083334075180140m,
            40082518437363480773582156135m,
            42251553976661079280769795468m,
            45393172934734752826080273238m,
            13439867486397701296318424200m,
            36643081827346056974829509710m,
            27982332852490435982401169009m,
            42867845691408051535046277740m,
            32313937823934069717723743363m,
            33858779218962836929213720922m,
            32607673426064364516200967016m,
            40701526421343915441700176763m,
            38220796916409913463426878338m,
            46296505728435224801158986613m,
            26777265610962158504622857361m,
            36025316034373388127172393827m,
            31080809801441928223550368881m,
            41318035994942441946311718511m,
            37595777286111216795169226621m,
            34504506446325449338790638201m,
            32008055757151261616764186230m,
            35099254967069289411849580138m,
            29851365280217876910430191735m,
            43170062905136476261956150622m,
            40741520366248297589663502729m,
            48762733937932907951436238731m,
            32645145515798586982325452453m,
            36969520414596807170008314739m,
            34800683864293243662065889139m,
            38218364545883237982786386555m,
            37910074166162077152473020027m,
            39441712362202179851444912001m,
            37590960398151412198611707501m,
            35108893779825105380735744366m,
            37590964990448292959146898806m,
            37588357952833910215500721262m,
            47224985366580905091061817708m,
            33543344119751465808960185440m,
            38220815472754118738212448108m,
            47207988832317751935279402091m,
            39156585966641357505203115425m,
            43183474611645056408599103084m,
            40399148680872958296277878155m,
            27051445638266748008850947449m,
            25803729873629449404239667800m,
            33571310140373359682770659692m,
            30750791775841337475349380201m,
            34789708457828663318817245257m,
            33565199714264861584472370788m,
            42563205830030198110079186538m,
            37925894482056208982422227083m,
            41009807062269881895187154047m,
            33573699640302844113553232772m,
        };

        public NegamaxV1()
        {
            entries = new Entry[TTSize];

            //pieceSquareTables = pieceSquareTablesCompressed.SelectMany(decimal.GetBits).Where((x, i) => i % 4 != 3).SelectMany(BitConverter.GetBytes).Select(x => x * 375 / 255 - 167 - 9).ToArray();
            pieceSquareTables = pieceSquareTablesCompressed
                .SelectMany(decimal.GetBits)
                .Where((x, i) => i % 4 != 3)
                .SelectMany(BitConverter.GetBytes)
                .Select(x => x * 375 / 255 - 176)
                .ToArray();

            for (int i = 0; i < 768; ++i)
                pieceSquareTables[i] += pieceValues[i / 128 + 1];
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

            //int eval = 0;
            //for (; !endSearch;)
            //{
            //    int currendEval = Search(currentDepth++, 0, -inf, inf);
            //    if (!endSearch) eval = currendEval;
            //}
            //Console.WriteLine(eval);

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

            //foreach (PieceList list in board.GetAllPieceLists())
            //    foreach (Piece piece in list) evaluation += pieceValues[(int)piece.PieceType] * (piece.IsWhite ? 1 : -1);

            // TODO: interpolation between midgame and endgame
            foreach (PieceList list in board.GetAllPieceLists())
                foreach (Piece piece in list) evaluation += pieceSquareTables[((int)piece.PieceType - 1) * 2 * 64 + (piece.IsWhite ? piece.Square.Index : piece.Square.Index ^ 56)] * (piece.IsWhite ? 1 : -1);

            if (!board.IsWhiteToMove) evaluation = -evaluation;

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
            if (endSearch || board.IsDraw()) return 0;
            if (board.IsInCheckmate()) return -10000000 - depth;

            bool qsearch = depth <= 0;
            int lookup;

            if (useTranspositionTable && ply > 0 &&
                (lookup = Lookup(depth, alpha, beta)) != lookupFailed)
            {
                ++positionsLookedUp;
                return lookup;
            }

            int type = Alpha;
            Move[] moves = board.GetLegalMoves(qsearch);
            OrderMoves(moves);

            if (qsearch)
            {
                int eval = Evaluate();
                if (eval >= beta)
                {
                    ++cutoffs;
                    return beta;
                }
                if (eval > alpha) alpha = eval;
                if (moves.Length == 0) return eval;
            }

            Move currentBestMove = moves[0];

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

        // Transposition Table
        public ulong Index
        {
            get => board.ZobristKey % TTSize;
        }

        public void Store(int depth, int value, Move move, int type)
        {
            //if (entries[board.ZobristKey % size].depth >= depth) return;

            entries[Index] = new(board.ZobristKey, depth, value, move, type);
        }

        public int Lookup(int depth, int alpha, int beta)
        {
            Entry entry = entries[Index];

            if (entry == null ||
                entry.key != board.ZobristKey ||
                entry.depth < depth) return lookupFailed;

            int type = entry.type, value = entry.value;
            if ((type == Exact) ||
                (type == Alpha && value <= alpha) ||
                (type == Beta && value >= beta)) return value;

            return lookupFailed;
        }

        public Move LookupMove() => entries[Index]?.move ?? Move.NullMove;
    }
}