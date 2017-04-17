using System;

namespace zChess
{
    public enum Colors { Black, White, nil }
    public enum Pieces { King, Queen, Rook, Bishop, Knight, Pawn };
    public enum Files { a, b, c, d, e, f, g, h };
    
    public abstract class chessman
    {
        public readonly static string[] strFiles = new string[8] { "a", "b", "c", "d", "e", "f", "g", "h" };

        protected static int Number = 0;

        protected Chessboard board;
        protected chessman[] teammates;
        protected chessman[] rivals;
        protected int pieceID = 0;
        protected sbyte pieceIndex;
        protected Colors Coalition;
        protected Colors RivalColor;
        protected Pieces Piece;
        protected bool Alive = false;
        protected sbyte[] position = new sbyte [2];
        protected bool initialPos = true;
        protected int firstStep = int.MaxValue;
        protected sbyte R0; // = 0 for White ; = 7 for Black
        protected byte capacity;

        public virtual string Symbol { get; set; }

        public sbyte[,] MovesArray { get; set; }

        public byte MaxMoves
        {
            get { return capacity; }
        }

        public string Chessboard
        {
            get { return board.name;  }
        }

        public int ID
        {
            get { return pieceID; }
        }

        public sbyte index
        {
            get { return pieceIndex; }
        }

        public Colors color
        {
            get { return Coalition; }
        }

        public Pieces piece
        {
            get { return Piece; }
        }

        public bool isAlive
        {
            get { return Alive; }
        }

        public sbyte iniRank
        {
            get { return R0; }
        }

        public sbyte[] posArr
        {
            get { return position; }
        }

        public string posStr
        {
            get
            {
                string file = strFiles[position[0]];
                string rank = (position[1] + 1).ToString();
                return (file + rank);
            }
        }

        public bool InInitial
        {
            get { return initialPos; }
        }

        public int FirstStep
        {
            get { return firstStep; }
        }

        public void capture()
        {
            Alive = false;
        }

        public void revive()
        {
            Alive = true;
        }

        public virtual byte tryCastling(byte xside) // method will be overridden in the derived class King
        {
            return 0;
        }

        public abstract sbyte[,] DefineMoves(out byte Num, int currStep = -1, bool refined = false);
        /*
         * returns movesArr = new int[k,2] ; k is number of possible moves
         * movesArr[k,0] is number of file
         * movesArr[k,1] is number of rank 
         * movesArr[k,0] = -128 means that k-move is not defined or prohibited   
         */

        public sbyte Move(int currStep, sbyte ifile, sbyte jrank, ref sbyte[,] movesArr, bool free = false)
        {
            sbyte success = -128;
			
            if (free)
            {
                position[0] = ifile;
                position[1] = jrank;
                success = -1;

                if ( (ifile < 0) || (jrank < 0) || (ifile > 7) || (jrank > 7) ) Alive = false; 
            }
            else
            {
				sbyte keyValue;
                byte num;

                if (movesArr == null)
                {
                    if (MovesArray == null)
                    {
                        MovesArray = DefineMoves(out num, currStep); // true number of valid moves (out num) is not used here
                    }

                    num = (byte)MovesArray.GetLength(0);
                    movesArr = MovesArray;
                }
                else
                {
                    num = (byte)movesArr.GetLength(0);
                }

                for (byte i = 0; i < num; i++)
                {
					keyValue = movesArr[i,0];
					
                    if ( keyValue >= 0 && keyValue == ifile && movesArr[i,1] == jrank )
                    {
                        position[0] = ifile;
                        position[1] = jrank;
                        success = (sbyte)i;
                        break;
                    }
                }
            } // end if (free)
            
            if (success != -128)
            {
                if (initialPos)
                {
                    firstStep = currStep;
                    initialPos = false;
                }
                else if (currStep == firstStep)
                {
                    firstStep = int.MaxValue;
                    initialPos = true;
                }
            }

            return success;
        } // end of public bool Move(...)

        public void AIMove(int currStep, sbyte ifile, sbyte jrank)
        {
            position[0] = ifile;
            position[1] = jrank;

            if (initialPos)
            {
                firstStep = currStep;
                initialPos = false;
            }
            else if (currStep == firstStep)
            {
                firstStep = int.MaxValue;
                initialPos = true;
            }

            return;
        } // end of public bool AIMove(...)

        public void SetState(bool alive, sbyte[] cell, bool initialPos, int firstStep) // method is needed to rewind journal
        {
            this.Alive = alive;
            this.position = cell;
            this.initialPos = initialPos;
            this.firstStep = firstStep;
        }

        /*public chessman(Colors color = Colors.Black, Pieces piece = Pieces.Pawn, sbyte[] cell = null)
        {
            pieceID = ++Number;
            Alive = true;
            Coalition = color;
            Piece = piece;
            position = cell ?? new sbyte[2];
        } // */
    } // end of public abstract class chessman //////////////////////////////////////////////////////////////////////////////

    public class King : chessman
    {
        private chessman KingsideRook = null;
        private chessman QueensideRook = null;
        private bool KingsideFeasible = true;
        private bool QueensideFeasible = true;

        public override string Symbol
        {
            get
            {
                return "K ";
            }
        }

        public static readonly sbyte[][] IniPosition = new sbyte[2][]
        {
            new sbyte[2] { 4, 7 }, // Black
            new sbyte[2] { 4, 0 } // White
        };

        public readonly int[,] shiftsArr;
        public readonly int[,] shiftsHemicycle;
        public readonly sbyte[,] KnightInvaders;
        
        public bool SetCastleRooks(byte xside = 0)
        {
            bool check01 = (xside == 0 || xside == 1);
            bool check02 = (xside == 0 || xside == 2);

            if (check01) // kingside Rook
            {
                KingsideFeasible = true;
                KingsideRook = teammates[3];

                if (KingsideRook == null || KingsideRook.piece != Pieces.Rook) // then try to find it in cell R0,7
                {
                    KingsideRook = board.Require(R0, 7);

                    if (KingsideRook == null || KingsideRook.color == RivalColor || KingsideRook.piece != Pieces.Rook ||
                        !KingsideRook.isAlive || !KingsideRook.InInitial)
                    {
                        KingsideFeasible = false;
                        KingsideRook = null;
                    }
                }
                else if (!KingsideRook.isAlive || !KingsideRook.InInitial) KingsideFeasible = false;
            }

            if (check02) // queenside Rook
            {
                QueensideFeasible = true;
                QueensideRook = teammates[2];

                if (QueensideRook == null || QueensideRook.piece != Pieces.Rook) // then try to find it in cell R0,0
                {
                    QueensideRook = board.Require(R0, 0);

                    if (QueensideRook == null || QueensideRook.color == RivalColor || QueensideRook.piece != Pieces.Rook ||
                        !QueensideRook.isAlive || !QueensideRook.InInitial)
                    {
                        QueensideFeasible = false;
                        QueensideRook = null;
                    }
                }
                else if (!QueensideRook.isAlive || !QueensideRook.InInitial) QueensideFeasible = false;
            }

            return ( (KingsideFeasible && check01) || (QueensideFeasible && check02) );
        }

        public override sbyte[,] DefineMoves(out byte ind, int currStep = -1, bool refined = false)
        { 
            sbyte[,] movesArr = new sbyte[8, 2];
            int ifile = position[0];
            int jrank = position[1];
            int i1;
            int j1;
            bool Flag0 = true;
            bool Flag4 = true;
            chessman Holder;
            
            ind = 0;
            
            for (int i = 0; i < 8; i++) // Define movesArr in such a way that as a result of the move the King would not be in check
            {
                i1 = ifile + shiftsArr[i, 0];

                if ( i1 >= 0 && i1 <= 7)
                {
                    j1 = jrank + shiftsArr[i, 1];

                    if ( j1 >= 0 && j1 <= 7)
                    {
                        Holder = board.Require( i1, j1 );

                        if (Holder == null || !Holder.isAlive || Holder.color != Coalition)
                        {
                            Alive = false; // for correct result of the (board.Require(_i1, _j1, Coalition) == null)

                            if (board.Require(i1, j1, Coalition) == null) // verify that the cell is not under attack of any rival  
                            {
                                movesArr[ind, 0] = (sbyte)i1;
                                movesArr[ind, 1] = (sbyte)j1;
                                ind++;

                                if (i == 0 && Holder != null && Holder.isAlive) Flag0 = false;
                                else if (i == 4 && Holder != null && Holder.isAlive) Flag4 = false;
                            }
                            else if (i == 0) Flag0 = false; // cell is under attack
                            else if (i == 4) Flag4 = false; // cell is under attack

                            Alive = true;
                        }
                        else if (i == 0) Flag0 = false; // cell owner is teammate
                        else if (i == 4) Flag4 = false; // cell owner is teammate
                    }
                }
            } // end for

            if (initialPos && (Flag0 || Flag4) && 
                board.Require(4, R0, Coalition, shiftsHemicycle, KnightInvaders) == null) // define permissions for the Castling
            {
                if (Flag4 && KingsideFeasible && (KingsideRook != null || SetCastleRooks(1)) &&
                    KingsideRook.isAlive && KingsideRook.InInitial &&
                    board.Require(6, R0) == null && board.Require(5, R0) == null &&
                    board.Require(5, R0, Coalition, shiftsHemicycle, KnightInvaders) == null &&
                    board.Require(6, R0, Coalition, shiftsHemicycle, KnightInvaders) == null) // teammates[3] h-Rook - kingside castling
                {
                    movesArr[ind, 0] = 6;
                    movesArr[ind, 1] = R0;
                    ind++;
                }

                if (Flag0 && QueensideFeasible && (QueensideRook != null || SetCastleRooks(2)) &&
                    QueensideRook.isAlive && QueensideRook.InInitial &&
                    board.Require(1, R0) == null && board.Require(2, R0) == null && board.Require(3, R0) == null &&
                    board.Require(3, R0, Coalition, shiftsHemicycle, KnightInvaders) == null &&
                    board.Require(2, R0, Coalition, shiftsHemicycle, KnightInvaders) == null) // teammates[2] a-Rook - queenside castling
                {
                    movesArr[ind, 0] = 2;
                    movesArr[ind, 1] = R0;
                    ind++;
                }
            }

            for (int i = ind; i < 8; i++) movesArr[i, 0] = -128; // fills the remaining elements with the values that are out of range

            return movesArr; 
        } // end of public override sbyte[,] DefineMoves

        public override byte tryCastling(byte xside = 0)
        {
            byte Num = 0;

            if (initialPos && board.Require(4, R0, Coalition, shiftsHemicycle, KnightInvaders) == null)
            {
                if ((xside == 0 || xside == 1) &&
                    KingsideFeasible && (KingsideRook != null || SetCastleRooks(1)) &&
                    KingsideRook.isAlive && KingsideRook.InInitial &&
                    board.Require(6, R0) == null && board.Require(5, R0) == null &&
                    board.Require(5, R0, Coalition, shiftsHemicycle, KnightInvaders) == null &&
                    board.Require(6, R0, Coalition, shiftsHemicycle, KnightInvaders) == null) // teammates[3] h-Rook - kingside castling
                {
                    Num = 1;
                }
                
                if ((xside == 0 || xside == 2) &&
                    QueensideFeasible && (QueensideRook != null || SetCastleRooks(2)) &&
                    QueensideRook.isAlive && QueensideRook.InInitial &&
                    board.Require(1, R0) == null && board.Require(2, R0) == null && board.Require(3, R0) == null &&
                    board.Require(3, R0, Coalition, shiftsHemicycle, KnightInvaders) == null &&
                    board.Require(2, R0, Coalition, shiftsHemicycle, KnightInvaders) == null) // teammates[2] a-Rook - queenside castling
                {
                    Num += 2;
                }
            }

            return Num; // 0: both are prohibited ; 1: kingside 0-0 ; 2: queenside 0-0-0 ; 3: both are allowed
        }

        public King(Chessboard Board, sbyte Ind, Colors color = Colors.Black, sbyte[] cell = null)
        {
            this.board = Board;
            pieceID = ++Number;
            pieceIndex = Ind;
            Alive = true;
            Coalition = color;
            Piece = Pieces.King;
            capacity = 8;
            
            if (color == Colors.Black)
            {
                position = new sbyte[2] { 4, 7 };
                R0 = 7;
                teammates = Board.BlackPieces;
                rivals = Board.WhitePieces;
                RivalColor = Colors.White;

                shiftsArr = new int[8, 2] { {-1, 0}, {-1, -1}, {0, -1}, {1, -1}, {1, 0}, {1, 1}, {0, 1}, {-1, 1} };
                shiftsHemicycle = new int[8, 2] { {85,85}, {-1, -1}, {0, -1}, {1, -1}, {85,85}, {85,85}, {85,85}, {85,85} }; // {85,85} to disable i-th direction
                KnightInvaders = new sbyte[4, 2] { { -2, -1 }, { -1, -2 }, { 1, -2 }, { 2, -1 } };
            }
            else // color is Colors.White
            {
                position = new sbyte[2] { 4, 0 };
                R0 = 0;
                teammates = Board.WhitePieces;
                rivals = Board.BlackPieces;
                RivalColor = Colors.Black;

                shiftsArr = new int[8, 2] { {-1, 0}, {-1, 1}, {0, 1}, {1, 1}, {1, 0}, {1, -1}, {0, -1}, {-1, -1} };
                shiftsHemicycle = new int[8, 2] { {85,85}, {-1, 1}, {0, 1}, {1, 1}, {85,85}, {85,85}, {85,85}, {85,85} };
                KnightInvaders = new sbyte[4, 2] { { -2, 1 }, { -1, 2 }, { 1, 2 }, { 2, 1 } };
            }

            if (cell == null)
            {
                initialPos = true;
                firstStep = int.MaxValue;
            }
            else // King has made a move before game start
            {
                position = cell;
                initialPos = false;
                firstStep = int.MinValue; 
            }
        }
    } // end of public class King //////////////////////////////////////////////////////////////////////////////////////////////

    public class Queen : chessman
    {
        public override string Symbol
        {
            get
            {
                return "Q ";
            }
        }

        public readonly int[,] shiftsArr;

        public override sbyte[,] DefineMoves(out byte Num, int currStep = -1, bool refined = false)
        {
            Num = 0;
            sbyte[,] movesArr = new sbyte[27, 2];

            int i1 = position[0];
            int j1 = position[1];
            int ifile = i1;
            int jrank = j1;
            int[,] RingCells = new int[8, 2] { { i1, j1}, { i1, j1 }, { i1, j1 }, { i1, j1 }, { i1, j1 }, { i1, j1 }, { i1, j1 }, { i1, j1 } };

            bool active = false;
            bool[] Bearings = new bool[8] { true, true, true, true, true, true, true, true };

            chessman Owner;

            for (int iter = 0; iter < 7; iter++) // Range expansion loop ----------------
            {
                for (int i = 0; i < 8; i++) // Bearing loop -----------------
                {
                    if (Bearings[i])
                    {
                        RingCells[i, 0] += shiftsArr[i, 0];
                        ifile = RingCells[i, 0];

                        if (ifile >= 0 && ifile <=7)
                        {
                            RingCells[i, 1] += shiftsArr[i, 1];
                            jrank = RingCells[i, 1];

                            if (jrank >= 0 && jrank <= 7)
                            {
                                Owner = board.Require( ifile, jrank );

                                if (Owner == null || !Owner.isAlive)
                                {
                                    active = true;

                                    if ( !refined || board.VerifyMove(this, i1, j1, ifile, jrank) )
                                    {
                                        movesArr[Num, 0] = (sbyte)ifile;
                                        movesArr[Num, 1] = (sbyte)jrank;
                                        Num++;   
                                    }   
                                }
                                else if (Owner.color == RivalColor)
                                {
                                    if ( !refined || board.VerifyMove(this, i1, j1, ifile, jrank) )
                                    {
                                        movesArr[Num, 0] = (sbyte)ifile;
                                        movesArr[Num, 1] = (sbyte)jrank;
                                        Num++;
                                    }     
                                }
                            } // end if (jrank >= 0 && jrank <= 7)
                        } // end if (ifile >= 0 && ifile <=7)
                    } // end if (Bearings[i])

                    Bearings[i] = active;
                    active = false;
                } // end of Bearing loop ------------------------------------
            } // end of Range expansion loop -------------------------------------------

            for (int ind = Num; ind < 27; ind++)
            {
                movesArr[ind, 0] = -128; // fills the remaining elements
            }

            return movesArr;
        }

        public Queen(Chessboard Board, sbyte Ind, Colors color = Colors.Black, sbyte[] cell = null)
        {
            this.board = Board;
            pieceID = ++Number;
            pieceIndex = Ind;
            Alive = true;
            Coalition = color;
            Piece = Pieces.Queen;
            capacity = 27;

            if (color == Colors.Black)
            {
                position = new sbyte[2] { 3, 7 };
                teammates = Board.BlackPieces;
                rivals = Board.WhitePieces;
                RivalColor = Colors.White;

                shiftsArr = new int[8, 2] { { -1, 0 }, { -1, -1 }, { 0, -1 }, { 1, -1 }, { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 1 } };
            }
            else // color is Colors.White
            {
                position = new sbyte[2] { 3, 0 };
                teammates = Board.WhitePieces;
                rivals = Board.BlackPieces;
                RivalColor = Colors.Black;

                shiftsArr = new int[8, 2] { { -1, 0 }, { -1, 1 }, { 0, 1 }, { 1, 1 }, { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, -1 } };
            }

            if (cell == null)
            {
                initialPos = true;
                firstStep = int.MaxValue;
            }
            else // Queen has made a move before game start
            {
                position = cell;
                initialPos = false;
                firstStep = int.MinValue;
            }
        }
    } // end of public class Queen ////////////////////////////////////////////////////////////////////////////////////////////

    public class Rook : chessman
    {
        public override string Symbol
        {
            get
            {
                return "R ";
            }
        }

        public static readonly sbyte[][,] IniPosition = new sbyte[2][,]
        {
            new sbyte[2, 2] { { 0, 7 }, { 7, 7 } }, // Black
            new sbyte[2, 2] { { 0, 0 }, { 7, 0 } } // White
        };

        public readonly int[,] shiftsArr;

        public override sbyte[,] DefineMoves(out byte Num, int currStep = -1, bool refined = false)
        {
            Num = 0;
            sbyte[,] movesArr = new sbyte[14, 2];

            int i1 = position[0];
            int j1 = position[1];
            int ifile = i1;
            int jrank = j1;
            int[,] RingCells = new int[4, 2] { { ifile, jrank }, { ifile, jrank }, { ifile, jrank }, { ifile, jrank } };

            bool active = false;
            bool[] Bearings = new bool[4] { true, true, true, true };

            chessman Owner;

            for (int iter = 0; iter < 7; iter++) // Range expansion loop ----------------
            {
                for (int i = 0; i < 4; i++) // Bearing loop -----------------
                {
                    if (Bearings[i])
                    {
                        RingCells[i, 0] += shiftsArr[i, 0];
                        ifile = RingCells[i, 0];

                        if (ifile >= 0 && ifile <= 7)
                        {
                            RingCells[i, 1] += shiftsArr[i, 1];
                            jrank = RingCells[i, 1];

                            if (jrank >= 0 && jrank <= 7)
                            {
                                Owner = board.Require( ifile, jrank );

                                if (Owner == null || !Owner.isAlive)
                                {
                                    active = true;

                                    if ( !refined || board.VerifyMove(this, i1, j1, ifile, jrank) )
                                    {
                                        movesArr[Num, 0] = (sbyte)ifile;
                                        movesArr[Num, 1] = (sbyte)jrank;
                                        Num++;  
                                    }    
                                }
                                else if (Owner.color == RivalColor)
                                {
                                    if ( !refined || board.VerifyMove(this, i1, j1, ifile, jrank) )
                                    {
                                        movesArr[Num, 0] = (sbyte)ifile;
                                        movesArr[Num, 1] = (sbyte)jrank;
                                        Num++;
                                    }       
                                }
                            } // end if (jrank >= 0 && jrank <= 7)
                        } // end if (ifile >= 0 && ifile <=7)
                    } // end if (Bearings[i])

                    Bearings[i] = active;
                    active = false;
                } // end of Bearing loop ------------------------------------
            } // end of Range expansion loop -------------------------------------------

            for (int ind = Num; ind < 14; ind++)
            {
                movesArr[ind, 0] = -128; // fills the remaining elements
            }

            return movesArr;
        }

        public Rook(Chessboard Board, sbyte Ind, Colors color = Colors.Black, sbyte[] cell = null)
        {
            this.board = Board;
            pieceID = ++Number;
            pieceIndex = Ind;
            Alive = true;
            Coalition = color;
            Piece = Pieces.Rook;
            capacity = 14;

            if (color == Colors.Black)
            {
                if (Ind == 2) position = new sbyte[2] { 0, 7 };
                else position = new sbyte[2] { 7, 7 };

                teammates = Board.BlackPieces;
                rivals = Board.WhitePieces;
                RivalColor = Colors.White;

                shiftsArr = new int[4, 2] { { -1, 0 }, { 0, -1 }, { 1, 0 }, { 0, 1 } };
            }
            else // color is Colors.White
            {
                if (Ind == 2) position = new sbyte[2] { 0, 0 };
                else position = new sbyte[2] { 7, 0 };

                teammates = Board.WhitePieces;
                rivals = Board.BlackPieces;
                RivalColor = Colors.Black;

                shiftsArr = new int[4, 2] { { -1, 0 }, { 0, 1 }, { 1, 0 }, { 0, -1 } };
            }

            if (cell == null)
            {
                initialPos = true;
                firstStep = int.MaxValue;
            }
            else // Rook has made a move before game start
            {
                position = cell;
                initialPos = false;
                firstStep = int.MinValue;
            }
        }
    } // end of public class Rook //////////////////////////////////////////////////////////////////////////////////////////////

    public class Bishop : chessman
    {
        public override string Symbol
        {
            get
            {
                return "B ";
            }
        }

        public readonly int[,] shiftsArr;

        public override sbyte[,] DefineMoves(out byte Num, int currStep = -1, bool refined = false)
        {
            Num = 0;
            sbyte[,] movesArr = new sbyte[13, 2];

            int i1 = position[0];
            int j1 = position[1];
            int ifile = i1;
            int jrank = j1;
            int[,] RingCells = new int[4, 2] { { ifile, jrank }, { ifile, jrank }, { ifile, jrank }, { ifile, jrank } };

            bool active = false;
            bool[] Bearings = new bool[4] { true, true, true, true };

            chessman Owner;

            for (int iter = 0; iter < 7; iter++) // Range expansion loop ----------------
            {
                for (int i = 0; i < 4; i++) // Bearing loop -----------------
                {
                    if (Bearings[i])
                    {
                        RingCells[i, 0] += shiftsArr[i, 0];
                        ifile = RingCells[i, 0];

                        if (ifile >= 0 && ifile <= 7)
                        {
                            RingCells[i, 1] += shiftsArr[i, 1];
                            jrank = RingCells[i, 1];

                            if (jrank >= 0 && jrank <= 7)
                            {
                                Owner = board.Require( ifile, jrank );

                                if (Owner == null || !Owner.isAlive)
                                {
                                    active = true;

                                    if ( !refined || board.VerifyMove(this, i1, j1, ifile, jrank) )
                                    {
                                        movesArr[Num, 0] = (sbyte)ifile;
                                        movesArr[Num, 1] = (sbyte)jrank;
                                        Num++;   
                                    }        
                                }
                                else if (Owner.color == RivalColor)
                                {
                                    if ( !refined || board.VerifyMove(this, i1, j1, ifile, jrank) )
                                    {
                                        movesArr[Num, 0] = (sbyte)ifile;
                                        movesArr[Num, 1] = (sbyte)jrank;
                                        Num++;
                                    }      
                                }
                            } // end if (jrank >= 0 && jrank <= 7)
                        } // end if (ifile >= 0 && ifile <=7)
                    } // end if (Bearings[i])

                    Bearings[i] = active;
                    active = false;
                } // end of Bearing loop ------------------------------------
            } // end of Range expansion loop -------------------------------------------

            for (int ind = Num; ind < 13; ind++)
            {
                movesArr[ind, 0] = -128; // fills the remaining elements
            }

            return movesArr;
        }

        public Bishop(Chessboard Board, sbyte Ind, Colors color = Colors.Black, sbyte[] cell = null)
        {
            this.board = Board;
            pieceID = ++Number;
            pieceIndex = Ind;
            Alive = true;
            Coalition = color;
            Piece = Pieces.Bishop;
            capacity = 13;

            if (color == Colors.Black)
            {
                if (Ind == 4) position = new sbyte[2] { 2, 7 };
                else position = new sbyte[2] { 5, 7 };

                teammates = Board.BlackPieces;
                rivals = Board.WhitePieces;
                RivalColor = Colors.White;

                shiftsArr = new int[4, 2] { { -1, -1 }, { 1, -1 }, { 1, 1 }, { -1, 1 } };
            }
            else // color is Colors.White
            {
                if (Ind == 4) position = new sbyte[2] { 2, 0 };
                else position = new sbyte[2] { 5, 0 };

                teammates = Board.WhitePieces;
                rivals = Board.BlackPieces;
                RivalColor = Colors.Black;

                shiftsArr = new int[4, 2] { { -1, 1 }, { 1, 1 }, { 1, -1 }, { -1, -1 } };
            }

            if (cell == null)
            {
                initialPos = true;
                firstStep = int.MaxValue;
            }
            else // Rook has made a move before game start
            {
                position = cell;
                initialPos = false;
                firstStep = int.MinValue;
            }
        }
    } // end of public class Bishop //////////////////////////////////////////////////////////////////////////////////////////////

    public class Knight : chessman
    {
        public override string Symbol
        {
            get
            {
                return "N ";
            }
        }

        public readonly sbyte[,] shiftsArr;

        public override sbyte[,] DefineMoves(out byte Num, int currStep = -1, bool refined = false)
        {
            Num = 0;
            sbyte[,] movesArr = new sbyte[8, 2];

            sbyte ifile = position[0];
            sbyte jrank = position[1];
            sbyte ihop = 0;
            sbyte jhop = 0;
            
            chessman Owner;

            for (int i = 0; i < 8; i++)
            {
                ihop = ifile;
                ihop += shiftsArr[i, 0];
                
                if (ihop >= 0 && ihop <= 7)
                {
                    jhop = jrank;
                    jhop += shiftsArr[i, 1];

                    if (jhop >= 0 && jhop <= 7)
                    {
                        Owner = board.Require(ihop, jhop);

                        if (Owner == null || !Owner.isAlive || Owner.color == RivalColor)
                        {
                            if ( !refined || board.VerifyMove(this, ifile, jrank, ihop, jhop) )
                            {
                                movesArr[Num, 0] = ihop;
                                movesArr[Num, 1] = jhop;
                                Num++;
                            }       
                        }    
                    } // end if (jhop >= 0 && jhop <= 7)
                } // end if (ihop >= 0 && ihop <= 7) 
            }

            for (int ind = Num; ind < 8; ind++)
            {
                movesArr[ind, 0] = -128; // fills the remaining elements
            }

            return movesArr;
        }

        public Knight(Chessboard Board, sbyte Ind, Colors color = Colors.Black, sbyte[] cell = null)
        {
            this.board = Board;
            pieceID = ++Number;
            pieceIndex = Ind;
            Alive = true;
            Coalition = color;
            Piece = Pieces.Knight;
            capacity = 8;

            if (color == Colors.Black)
            {
                if (Ind == 6) position = new sbyte[2] { 1, 7 };
                else position = new sbyte[2] { 6, 7 };

                teammates = Board.BlackPieces;
                rivals = Board.WhitePieces;
                RivalColor = Colors.White;

                shiftsArr = new sbyte[8, 2] { { -2, -1 }, { -1, -2 }, { 1, -2 }, { 2, -1 }, { 2, 1 }, { 1, 2 }, { -1, 2 }, { -2, 1 } };
            }
            else // color is Colors.White
            {
                if (Ind == 6) position = new sbyte[2] { 1, 0 };
                else position = new sbyte[2] { 6, 0 };

                teammates = Board.WhitePieces;
                rivals = Board.BlackPieces;
                RivalColor = Colors.Black;

                shiftsArr = new sbyte[8, 2] { { -2, 1 }, { -1, 2 }, { 1, 2 }, { 2, 1 }, { 2, -1 }, { 1, -2 }, { -1, -2 }, { -2, -1 } };
            }

            if (cell == null)
            {
                initialPos = true;
                firstStep = int.MaxValue;
            }
            else // Rook has made a move before game start
            {
                position = cell;
                initialPos = false;
                firstStep = int.MinValue;
            }
        }
    } // end of public class Knight //////////////////////////////////////////////////////////////////////////////////////////////

    public class Pawn : chessman
    {
        public override string Symbol
        {
            get
            {
                return "p ";
            }
        }

        public static readonly sbyte[] IniPosition = new sbyte[2] { 6, 1 }; // { BlackLine, WhiteLine }

        public readonly sbyte[,] shiftsArr;
        public readonly sbyte PassantRank;

        public override sbyte[,] DefineMoves(out byte Num, int currStep = -1, bool refined = false)
        {
            Num = 0;
            sbyte[,] movesArr = new sbyte[4, 2];

            if (currStep < 0) currStep = board.Scoresheet.StepNumber;

            sbyte ifile = position[0];
            sbyte jrank = position[1];
            sbyte i1;
            sbyte j1 = jrank;
            sbyte jshift = shiftsArr[0, 1];

            chessman Owner = null;

            for (byte i = 0; i < 2; i++) // free forward move
            {
                if ( i == 0 || (initialPos && (Owner == null || !Owner.isAlive)) )
                {
                    j1 += jshift;

                    if (j1 >= 0 && j1 <= 7)
                    {
                        Owner = board.Require(ifile, j1);

                        if (Owner == null || !Owner.isAlive)
                        {
                            if ( !refined || board.VerifyMove(this, ifile, jrank, ifile, j1) )
                            {
                                movesArr[Num, 0] = ifile;
                                movesArr[Num, 1] = j1;
                                Num++;
                            }      
                        }
                    }
                }   
            }

            j1 = jrank;
            j1 += jshift;

            for (byte i = 0; i < 2; i++) // diagonally capturing and en passant
            {
                i1 = ifile;
                i1 += shiftsArr[i, 0];

                if (i1 >= 0 && i1 <= 7)
                {
                    Owner = board.Require(i1, j1);

                    if (Owner != null && Owner.isAlive && Owner.color == RivalColor)
                    {
                        if ( !refined || board.VerifyMove(this, ifile, jrank, i1, j1) )
                        {
                            movesArr[Num, 0] = i1;
                            movesArr[Num, 1] = j1;
                            Num++;
                        }        
                    }
                    else if (jrank == PassantRank)
                    {
                        Owner = board.Require(i1, jrank);

                        if (Owner != null && Owner.isAlive && Owner.piece == Pieces.Pawn && Owner.color == RivalColor &&
                            currStep - Owner.FirstStep == 1)
                        {
                            if ( !refined || board.VerifyMove(this, ifile, jrank, i1, j1, enpassant: true) )
                            {
                                movesArr[Num, 0] = i1;
                                movesArr[Num, 1] = j1;
                                Num++;
                            }       
                        }
                    }
                } // end if (i1 >= 0 && i1 <= 7)
            }

            for (int ind = Num; ind < 4; ind++)
            {
                movesArr[ind, 0] = -128; // fills the remaining elements
            }

            return movesArr;
        }

        public Pawn(Chessboard Board, sbyte Ind, Colors color = Colors.Black, sbyte[] cell = null)
        {
            this.board = Board;
            pieceID = ++Number;
            pieceIndex = Ind;
            Alive = true;
            Coalition = color;
            Piece = Pieces.Pawn;
            capacity = 4;

            if (color == Colors.Black)
            {
                if (Ind >= 8 && Ind <= 15) position = new sbyte[2] { (sbyte)(Ind - 8), 6 };
                else position = new sbyte[2] { (sbyte)(Ind - 8), 5 };

                teammates = Board.BlackPieces;
                rivals = Board.WhitePieces;
                RivalColor = Colors.White;

                PassantRank = 3;
                shiftsArr = new sbyte[2, 2] { { -1, -1 }, { 1, -1 } };
            }
            else // color is Colors.White
            {
                if (Ind >= 8 && Ind <= 15) position = new sbyte[2] { (sbyte)(Ind - 8), 1 };
                else position = new sbyte[2] { (sbyte)(Ind - 8), 2 };

                teammates = Board.WhitePieces;
                rivals = Board.BlackPieces;
                RivalColor = Colors.Black;

                PassantRank = 4;
                shiftsArr = new sbyte[2, 2] { { -1, 1 }, { 1, 1 } };
            }

            if (cell == null)
            {
                initialPos = true;
                firstStep = int.MaxValue;
            }
            else // Rook has made a move before game start
            {
                position = cell;
                initialPos = false;
                firstStep = int.MinValue;
            }
        }
    }

    public class Journal
    {
        private int stepTot = 0;
        private int stepCurr = 0;
        private int turnCurr = 0;
        private int stepGameOver = Int32.MaxValue;
        private bool GameOver = false;
        private string[] StepInfo;
        private Chessboard Board;
        private sbyte[][] Scoresheet;
        /* Scoresheet[ind] = {
             *[0] i1, j1, i2, j2,               -- move vector | i1 == 127 "win end" ; i1 == 126 "patt end"
             *[4] -2 | -1 | 0 | 1 | 2 | 3       -- QS castling | KS castling | _ | capture | promotion | capture + promotion
             *[5] 0 ... 23                      -- index of the captured piece
             *[6] 0 ... 23                      -- index of the promoted pawn
             *[7] 0 ...  4                      -- enum of the replaced piece, 0 - no promotion
             * } */

        public ChessAI AIdata;

        public bool isReady = true;

        public sbyte[] StepDataTmp;

        public bool isOver
        {
            get { return GameOver; }
        }

        public int LastStep
        {
            get { return stepGameOver; }
        }

        public string firstLine
        {
            get
            {
                if (AIdata != null)
                {
                    if (AIdata.infoDesc != "")
                    {
                        return String.Format("Scoresheet and {0} :", AIdata.infoDesc);
                    }
                    else
                    {
                        return "Scoresheet :";
                    }
                }
                else
                {
                    return "";
                }
            }
        }
        
        public sbyte[][] Protocol
        {
            get { return Scoresheet; }
        }

        public string[] StringProtocol
        {
            get { return StepInfo; }
        }

        public int StepNumber
        {
            get { return stepCurr; }
        }

        public int StepTotal
        {
            get { return stepTot; }
        }

        public int TurnNumber
        {
            get { return turnCurr; }
        }

        public int duration
        {
            get { return Scoresheet.Length; }
            set
            {
                if (stepTot == 0)
                {
                    Scoresheet = new sbyte[value][];

                    if (value % 2 == 0)
                    {
                        StepInfo = new string[value];
                    }
                    else
                    {
                        StepInfo = new string[++value / 2];
                    }
                }
            }
        }

        public string infoLine(int ind)
        {
            if (ind < StepInfo.GetLength(0) && ind >= 0)
            {
                return StepInfo[ind];
            }
            else
            {
                return "";
            }
        }

        public bool Load(sbyte[][] scoreArr, int addNum = 200, Colors color = Colors.White)
        {
            bool done = true; // maybe it will be used with try-catch clause

            stepTot = scoreArr.GetLength(0);
            int totNum = stepTot + addNum;
            
            Scoresheet = new sbyte[totNum][];
            StepInfo = new string[totNum / 2];
            
            bool WinMark = scoreArr[stepTot - 1][0] == 127;
            bool PattMark = scoreArr[stepTot - 1][0] == 126;

            if (WinMark || PattMark)
            {
                stepTot--;
                stepGameOver = stepTot - 1;
                GameOver = true;
            }
            else // Game was not finished
            {
                stepGameOver = Int32.MaxValue;
                GameOver = false;

                if ((color == Colors.White && stepTot % 2 != 0) ||
                    (color == Colors.Black && stepTot % 2 == 0) )
                {
                    stepTot--;
                }
            }

            // Scoresheet restoring ----------------------------------------------------------------------------------------
            int stepStop = stepTot;
            bool oddFlag = false;

            if (stepTot % 2 != 0)
            {
                stepStop--;
                oddFlag = true;
            }

            stepCurr = 0;
            turnCurr = 0;

            for (int i = 0; i < stepStop; i += 2)
            {
                stepCurr = i + 1;

                Board.RedoMove(scoreArr[i], i);
                Board.RedoMove(scoreArr[stepCurr], stepCurr);

                Scoresheet[i] = scoreArr[i];
                Scoresheet[stepCurr] = scoreArr[stepCurr];

                StepInfo[turnCurr++] = 
                    String.Format("  {0,3}. {1}  |    {2}", turnCurr, VectorToString(scoreArr[i], true), VectorToString(scoreArr[stepCurr], false));
            }

            stepCurr++;

            if (oddFlag)
            {
                stepCurr++;

                Board.RedoMove(scoreArr[stepStop], stepStop);

                Scoresheet[stepStop] = scoreArr[stepStop];
                StepInfo[turnCurr] = String.Format("  {0,3}. {1}  |    ____  ____", turnCurr + 1, VectorToString(scoreArr[stepStop], true));
            }

            if (GameOver)
            {
                if (oddFlag) // White made last step
                {
                    turnCurr++;

                    if (WinMark) StepInfo[turnCurr] = "      * * *  White: 1 - Black: 0  * * *";
                    else StepInfo[turnCurr] = "      * * *  White: 0.5 - Black: 0.5  * * *";
                }
                else // Black made last step
                {
                    if (WinMark) StepInfo[turnCurr] = "      * * *  White: 0 - Black: 1  * * *";
                    else StepInfo[turnCurr] = "      * * *  White: 0.5 - Black: 0.5  * * *";
                }
            }

            return done;
        }

        public string VectorToString(sbyte[] StepData, bool whiteTurn)
        {
            string[] fileStr = chessman.strFiles;
            string vectorStr = "", Victim;
            string[] pieceSymbols = new string[5] { "", "Q", "R", "B", "N" };

            sbyte[] rivalKingPos = null;
            bool inShah = false;
            string shahSign = " ";

            if (whiteTurn)
            {
                if (Board.BlackPieces[0] != null)
                {
                    rivalKingPos = Board.BlackPieces[0].posArr;

                    if (Board.Require(rivalKingPos[0], rivalKingPos[1], Colors.Black) != null) inShah = true;
                }
            }
            else // black turn
            {
                if (Board.WhitePieces[0] != null)
                {
                    rivalKingPos = Board.WhitePieces[0].posArr;

                    if (Board.Require(rivalKingPos[0], rivalKingPos[1], Colors.White) != null) inShah = true;
                }
            }

            if (inShah) shahSign = "+";

            sbyte action = StepData[4];

            switch(action)
            {
                case -2:
                    vectorStr = "0-0-0      ";
                    break;

                case -1:
                    vectorStr = "0-0        ";
                    break;

                case 1: // capture

                    if (whiteTurn)
                    {
                        Victim = Board.BlackPieces[StepData[5]].Symbol;
                    }
                    else
                    {
                        Victim = Board.WhitePieces[StepData[5]].Symbol;
                    }

                    vectorStr = String.Format("{0}{1}{2}{3}{4} x{5}  ", 
                        fileStr[StepData[0]], StepData[1] + 1, fileStr[StepData[2]], StepData[3] + 1, shahSign, Victim);
                    break;

                case 2: // promotion
                    vectorStr = String.Format("{0}{1}{2}{3}{4} ={5}   ",
                        fileStr[StepData[0]], StepData[1] + 1, fileStr[StepData[2]], StepData[3] + 1, shahSign, pieceSymbols[StepData[7]]);
                    break;

                case 3: // capture and promotion

                    if (whiteTurn)
                    {
                        Victim = Board.BlackPieces[StepData[5]].Symbol;
                    }
                    else
                    {
                        Victim = Board.WhitePieces[StepData[5]].Symbol;
                    }
                    vectorStr = String.Format("{0}{1}{2}{3}{4} x{5}={6}", 
                        fileStr[StepData[0]], StepData[1] + 1, fileStr[StepData[2]], StepData[3] + 1, shahSign, Victim, pieceSymbols[StepData[7]]);
                    break;

                default:
                    vectorStr = String.Format("{0}{1}{2}{3}{4}      ", 
                        fileStr[StepData[0]], StepData[1] + 1, fileStr[StepData[2]], StepData[3] + 1, shahSign);
                    break;
            }
            
            return vectorStr;
        }
        
        /// <summary>
        /// Should called before Write()
        /// </summary>
        public void Promotion(Pieces Piece)
        {
            if (stepCurr > 0)
            {
                if (StepDataTmp != null && StepDataTmp.GetLength(0) > 7) StepDataTmp[7] = (sbyte)Piece;
            }
        }

        public void Write(sbyte[] StepData = null, bool useTmp = false, bool TeamZero = false, bool RivalZero = false)
        {
            // preliminary check -----------------------
            if (stepCurr <= stepGameOver)
            {
                stepGameOver = Int32.MaxValue;
                GameOver = false;
            }

            if (GameOver || stepCurr >= Scoresheet.GetLength(0) - 1 || turnCurr >= StepInfo.GetLength(0) - 1) return; // >>>>> NOT WRITE >>>>>

            stepTot = stepCurr; // it is needed for rewriting after rewinding

            if (useTmp) StepData = StepDataTmp;
            // end of preliminary check -----------------


            if (stepCurr % 2 == 0) // White's Turn -----------------------------------------------------------------------------------------
            {
                if (TeamZero)
                {
                    GameOver = true;
                    stepTot++;
                    stepGameOver = stepCurr - 1;

                    chessman WhiteKing = Board.WhitePieces[0];

                    if (WhiteKing != null)
                    {
                        if (Board.Require(WhiteKing.posArr[0], WhiteKing.posArr[1], Colors.White) != null)
                        {
                            StepInfo[turnCurr] = "      * * *  White: 0 - Black: 1  * * *";
                            Scoresheet[stepCurr] = new sbyte[1] { 127 };
                        }
                        else // Patt
                        {
                            StepInfo[turnCurr] = "      * * *  White: 0.5 - Black: 0.5  * * *";
                            Scoresheet[stepCurr] = new sbyte[1] { 126 };
                        }
                    }
                    else
                    {
                        StepInfo[turnCurr] = "      * * *  White can't move  * * *";
                        Scoresheet[stepCurr] = new sbyte[1] { 126 };
                    }
                }
                else if (RivalZero)
                {
                    GameOver = true;
                    stepGameOver = stepCurr;
                    Scoresheet[stepCurr] = StepData;
                    stepTot += 2;

                    StepInfo[turnCurr] = String.Format("  {0,3}. {1}", turnCurr + 1, VectorToString(StepData, true));

                    if (AIdata != null)
                    {
                        StepInfo[turnCurr] += String.Format(" - {0}    |    ____  ____  - {1}", AIdata.infoWhite, AIdata.infoBlack);
                    }

                    turnCurr++;

                    chessman BlackKing = Board.BlackPieces[0];

                    if (BlackKing != null)
                    {
                        if (Board.Require(BlackKing.posArr[0], BlackKing.posArr[1], Colors.Black) != null)
                        {
                            StepInfo[turnCurr] = "      * * *  White: 1 - Black: 0  * * *";
                            Scoresheet[stepCurr + 1] = new sbyte[1] { 127 };
                        }
                        else // Patt
                        {
                            StepInfo[turnCurr] = "      * * *  White: 0.5 - Black: 0.5  * * *";
                            Scoresheet[stepCurr + 1] = new sbyte[1] { 126 };
                        }
                    }
                    else
                    {
                        StepInfo[turnCurr] = "      * * *  Black can't move  * * *";
                        Scoresheet[stepCurr + 1] = new sbyte[1] { 126 };
                    }
                }
                else // ------- Write White's move -------
                {
                    StepInfo[turnCurr] = String.Format("  {0,3}. {1}", turnCurr + 1, VectorToString(StepData, true));

                    if (AIdata != null)
                    {
                        StepInfo[turnCurr] += String.Format(" - {0}    |    ____  ____  - {1}", AIdata.infoWhite, AIdata.infoBlack);
                    }
                }
            }
            else // Black's Turn ------------------------------------------------------------------------------------------------------
            {
                if (TeamZero)
                {
                    GameOver = true;
                    stepGameOver = stepCurr - 1;
                    stepTot++;

                    if (AIdata != null)
                    {
                        int len = StepInfo[turnCurr].Length;
                        string sub = StepInfo[turnCurr].Substring(0, len - 3);

                        StepInfo[turnCurr] = sub + "  0";
                    }

                    turnCurr++;

                    chessman blackKing = Board.BlackPieces[0];

                    if (blackKing != null)
                    {
                        if (Board.Require(blackKing.posArr[0], blackKing.posArr[1], Colors.Black) != null)
                        {
                            StepInfo[turnCurr] = "      * * *  White: 1 - Black: 0  * * *";
                            Scoresheet[stepCurr] = new sbyte[1] { 127 };
                        }
                        else // Patt
                        {
                            StepInfo[turnCurr] = "      * * *  White: 0.5 - Black: 0.5  * * *";
                            Scoresheet[stepCurr] = new sbyte[1] { 126 };
                        }
                    }
                    else
                    {
                        StepInfo[turnCurr] = "      * * *  Black can't move  * * *";
                        Scoresheet[stepCurr] = new sbyte[1] { 126 };
                    }
                }
                else if (RivalZero)
                {
                    GameOver = true;
                    stepGameOver = stepCurr;
                    Scoresheet[stepCurr] = StepData;
                    stepTot += 2;

                    if (AIdata != null)
                    {
                        int _Ind = StepInfo[turnCurr].IndexOf("|");
                        string Subs = StepInfo[turnCurr].Substring(0, ++_Ind);

                        StepInfo[turnCurr] = String.Format("{0}    {1} - {2}", Subs, VectorToString(StepData, false), AIdata.infoBlack);
                    }
                    else
                    {
                        StepInfo[turnCurr] += String.Format("  |  {0}", VectorToString(StepData, false));
                    }

                    turnCurr++;

                    chessman whiteKing = Board.WhitePieces[0];

                    if (whiteKing != null)
                    {
                        if (Board.Require(whiteKing.posArr[0], whiteKing.posArr[1], Colors.White) != null)
                        {
                            StepInfo[turnCurr] = "      * * *  White: 0 - Black: 1  * * *";
                            Scoresheet[stepCurr + 1] = new sbyte[1] { 127 };
                        }
                        else // Patt
                        {
                            StepInfo[turnCurr] = "      * * *  White: 0.5 - Black: 0.5  * * *";
                            Scoresheet[stepCurr + 1] = new sbyte[1] { 126 };
                        }
                    }
                    else
                    {
                        StepInfo[turnCurr] = "      * * *  White can't move  * * *";
                        Scoresheet[stepCurr + 1] = new sbyte[1] { 126 };
                    }
                }
                else // ------- Write Black's move -------
                {
                    if (AIdata != null)
                    {
                        int _Ind = StepInfo[turnCurr].IndexOf("|");
                        string Subs = StepInfo[turnCurr].Substring(0, ++_Ind);

                        StepInfo[turnCurr] = String.Format("{0}    {1} - {2}", Subs, VectorToString(StepData, false), AIdata.infoBlack);
                    }
                    else
                    {
                        StepInfo[turnCurr] += String.Format("  |  {0}", VectorToString(StepData, false));
                    }

                    turnCurr++;
                    StepInfo[turnCurr] = ""; // to clear old entry after rewinding
                }
            } // end if (stepCurr % 2 == 0)

            if (!GameOver)
            {
                Scoresheet[stepCurr] = StepData;

                stepCurr++;
                stepTot++;
            }
        }

        /// <summary>
        /// Method traverses scoresheet entries
        /// </summary>
        /// <param name="stroke">zero based index of the turn to rewind</param>
        /// <param name="color">color of the player is needed to determine step where should stop rewinding</param>
        /// <returns></returns>
        public string Rewind(int stroke, Colors color)
        {
            string StepRW = "_";
            bool? redoMode = null;

            if (stroke > turnCurr) redoMode = true;
            else if (stroke < turnCurr) redoMode = false;
            
            if (redoMode != null) // stroke != turnCurr
            {
                int stepAim = 2*stroke;
                int stepStart = 2*turnCurr;
                int stepMax = 0;

                if (GameOver)
                {
                    stepMax = stepGameOver + 1;
                }
                else
                {
                    stepMax = stepTot;
                }

                if (color == Colors.Black)
                {
                    stepAim++;
                    stepStart++;

                    if (GameOver && redoMode == false && stepAim > stepGameOver) stepAim -= 2;

                    if (stepAim < 1) stepAim = 1;
                    else if (stepAim > stepMax) stepAim = stepMax;
                }
                else // color is White
                {
                    if (GameOver && redoMode == false && stepAim > stepGameOver) stepAim -= 2;

                    if (stepAim < 0) stepAim = 0;
                    else if (stepAim > stepMax) stepAim = stepMax;
                } // end if (color == Colors.Black)

                if (GameOver && stepStart > stepGameOver) stepStart = stepGameOver + 1;

                // Rewinding ----------------------------------------------------------
                if (redoMode == true) 
                {
                    for (int i = stepStart; i < stepAim; i++)
                    {
                        Board.RedoMove(Scoresheet[i], i);
                    }
                }
                else
                {
                    for (int i = stepStart - 1; i >= stepAim; i--)
                    {
                        Board.UndoMove(Scoresheet[i], i);
                    }
                } // end of rewinding --------------------------------------------------

                stepCurr = stepAim;

                if (stepCurr % 2 == 0)
                {
                    turnCurr = stepCurr/2;
                }
                else
                {
                    turnCurr = (stepCurr - 1)/2;

                    if (GameOver && stepCurr > stepGameOver) turnCurr++;
                }
                
                if (AIdata != null) AIdata.RestoreData();
            } // end if (stroke != turnCurr)

            if (Scoresheet[stepCurr] != null && Scoresheet[stepCurr][0] != 127)
            {
                StepRW = VectorToString(Scoresheet[stepCurr], (color == Colors.White));
            }

            return StepRW;
        }

        public Journal(Chessboard board, int Num = 500)
        {
            Scoresheet = new sbyte[Num][];

            if (Num % 2 != 0) Num++;

            StepInfo = new string[Num / 2];

            Board = board;
        }
    } // end of public class Journal ////////////////////////////////////////////////////////////////////////////////////////////

    public class Chessboard
    {
        private string Name;
        private chessman[,] CheckersField = new chessman[8, 8];
        private chessman[][] PiecesArr = { new chessman[24], new chessman[24] }; // { BlackPieces, WhitePieces }

        public readonly Journal Scoresheet;

        /*public readonly sbyte[][,] InitialCoords =
        {
            new sbyte[16, 2] {  {4, 7}, {3, 7}, {0, 7}, {7, 7}, {2, 7}, {5, 7}, {1, 7}, {6, 7},
                                {0, 6}, {1, 6}, {2, 6}, {3, 6}, {4, 6}, {5, 6}, {6, 6}, {7, 6} }, // BlackPieces coords
            new sbyte[16, 2] {  {4, 0}, {3, 0}, {0, 0}, {7, 0}, {2, 0}, {5, 0}, {1, 0}, {6, 0},
                                {0, 1}, {1, 1}, {2, 1}, {3, 1}, {4, 1}, {5, 1}, {6, 1}, {7, 1} } // WhitePieces coords
        }; // */

        public readonly int[,] shiftersBlack = new int[8, 2] { { -1, 0 }, { -1, -1 }, { 0, -1 }, { 1,-1 }, { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 1 } };
        public readonly int[,] shiftersWhite = new int[8, 2] { { -1, 0 }, { -1,  1 }, { 0,  1 }, { 1, 1 }, { 1, 0 }, { 1,-1 }, { 0,-1 }, { -1,-1 } };

        public readonly sbyte[,] knightsBlack = new sbyte[8, 2] { { -2,-1 }, { -1,-2 }, { 1,-2 }, { 2,-1 }, { 2, 1 }, { 1, 2 }, { -1, 2 }, { -2, 1 } };
        public readonly sbyte[,] knightsWhite = new sbyte[8, 2] { { -2, 1 }, { -1, 2 }, { 1, 2 }, { 2, 1 }, { 2,-1 }, { 1,-2 }, { -1,-2 }, { -2,-1 } };

        public chessman[] BlackPieces { get { return PiecesArr[0]; } }
        public chessman[] WhitePieces { get { return PiecesArr[1]; } }

        public string name
        {
            get { return Name; }
        }

        public chessman[] GetPieces(Colors color = Colors.Black)
        {
            if (color == Colors.Black) return PiecesArr[0];
            else return PiecesArr[1];
        }

        /// <summary>
        /// Finds threats for Coalition or return cell owner
        /// </summary>
        public chessman Require( int ifile = 0, int jrank = 0, Colors Coalition = Colors.nil, 
            int[,] Shifters = null, sbyte[,] KnightCells = null, chessman[] Invaders = null) // finds threats for coalition or return cell owner
        {
            /* Finds threats for Coalition or return cell owner
             * Shifters template: Shifters = new int[8, 2] { {100,100}, {-1, -1}, {0, -1}, {1, -1}, {100,100}, {100,100}, {100,100}, {100,100} }
             * {100,100} allows to reduce number of scanning directions, only 3 instead of 8 in the example above
             */
            Colors threatColor;

            switch (Coalition)
            {
                case Colors.Black: // find White invaders
                    Shifters = Shifters ?? shiftersBlack;
                    KnightCells = KnightCells ?? knightsBlack;
                    threatColor = Colors.White;
                    break;

                case Colors.White: // find Black invaders
                    Shifters = Shifters ?? shiftersWhite;
                    KnightCells = KnightCells ?? knightsWhite;
                    threatColor = Colors.Black;
                    break;

                default: // return cell owner
                    return CheckersField[ifile, jrank]; // >>>>> RETURN >>>>>
            }

            int quantity = 0, indInv = 0;
            if (Invaders != null) quantity = Invaders.Length - 1;

            int knightsNum = KnightCells.GetLength(0);
            int iShift = 0;
            int jShift = 0;

            int[][] ScanCells = new int[8][];
            int scanNum = 0;
            bool foundFlag = false;
            bool[] Bearings = new bool[8];

            chessman invader = null;
            chessman FoundInvader = null;

            for (int i = 0; i < 8; i++) // Extend scan by one shifter and check Knights cells ---------------------------------------------------
            {
                iShift = ifile + Shifters[i, 0];

                if (iShift >= 0 && iShift <= 7)
                {
                    jShift = jrank + Shifters[i, 1];

                    if (jShift >= 0 && jShift <=7)
                    {
                        invader = CheckersField[ iShift, jShift ];

                        if ( (invader != null && invader.isAlive) && invader.color == threatColor )
                        {
                            switch(i)
                            {
                                case 0:
                                case 2:
                                case 4:
                                case 6:
                                    if (invader.piece == Pieces.King || invader.piece == Pieces.Queen || invader.piece == Pieces.Rook)
                                    {
                                        foundFlag = true;
                                    }
                                    break;
                                case 1:
                                case 3:
                                    if (invader.piece == Pieces.King || invader.piece == Pieces.Queen || invader.piece == Pieces.Bishop || invader.piece == Pieces.Pawn)
                                    {
                                        foundFlag = true;
                                    }
                                    break;
                                case 5:
                                case 7:
                                    if (invader.piece == Pieces.King || invader.piece == Pieces.Queen || invader.piece == Pieces.Bishop)
                                    {
                                        foundFlag = true;
                                    }
                                    break;
                            } // end switch(i)

                            if (foundFlag)
                            {
                                foundFlag = false;

                                if (indInv == quantity)
                                {
                                    if (Invaders != null) Invaders[indInv] = invader;
                                    return invader; // >>>>> RETURN >>>>>
                                }
                                else 
                                {
                                    Invaders[indInv] = invader;
                                    FoundInvader = invader;
                                    indInv++;
                                }
                            }
                            
                        }
                        else if (invader == null || !invader.isAlive)
                        {
                            ScanCells[scanNum] = new int[3] { iShift, jShift, i };
                            Bearings[scanNum] = true;
                            scanNum++;
                        }

                    } // end if (jShift >= 0)
                } // end if (iShift >= 0)

                if (i < knightsNum)
                {
                    iShift = ifile + KnightCells[i, 0];

                    if (iShift >= 0 && iShift <= 7)
                    {
                        jShift = jrank + KnightCells[i, 1];

                        if (jShift >= 0 && jShift <= 7)
                        {
                            invader = CheckersField[iShift, jShift];

                            if ((invader != null && invader.isAlive) && invader.color == threatColor && invader.piece == Pieces.Knight)
                            {
                                if (indInv == quantity)
                                {
                                    if (Invaders != null) Invaders[indInv] = invader;
                                    return invader; // >>>>> RETURN >>>>>
                                }
                                else
                                {
                                    Invaders[indInv] = invader;
                                    FoundInvader = invader;
                                    indInv++;
                                }
                            }

                        } // end if (jShift >= 0 && jShift <= 7)
                    } // end if (iShift >= 0 && iShift <= 7)


                } // if (i < knightsNum)
            } // end for ---------------------------------------------------------------------------------------------------

            bool active = false;
            int bearing = 0;

            for (int iter = 0; iter < 6; iter++) // Range expansion loop ----------------
            {
                for (int i = 0; i < scanNum; i++) // Bearing loop ---------------
                {
                    if (Bearings[i])
                    {
                        bearing = ScanCells[i][2];

                        ScanCells[i][0] += Shifters[bearing, 0];
                        iShift = ScanCells[i][0];

                        if (iShift >= 0 && iShift <= 7)
                        {
                            ScanCells[i][1] += Shifters[bearing, 1];
                            jShift = ScanCells[i][1];

                            if (jShift >= 0 && jShift <= 7)
                            {
                                invader = CheckersField[ iShift, jShift ];

                                if ((invader != null && invader.isAlive) && invader.color == threatColor)
                                {
                                    switch (bearing)
                                    {
                                        case 0:
                                        case 2:
                                        case 4:
                                        case 6:
                                            if (invader.piece == Pieces.Queen || invader.piece == Pieces.Rook)
                                            {
                                                foundFlag = true;
                                            }
                                            break;
                                        case 1:
                                        case 3:
                                        case 5:
                                        case 7:
                                            if (invader.piece == Pieces.Queen || invader.piece == Pieces.Bishop)
                                            {
                                                foundFlag = true;
                                            }
                                            break;
                                    } // end switch(i)

                                    if (foundFlag)
                                    {
                                        foundFlag = false;

                                        if (indInv == quantity)
                                        {
                                            if (Invaders != null) Invaders[indInv] = invader;
                                            return invader; // >>>>> RETURN >>>>>
                                        }
                                        else
                                        {
                                            Invaders[indInv] = invader;
                                            FoundInvader = invader;
                                            indInv++;
                                        }
                                    }

                                }
                                else if (invader == null || !invader.isAlive)
                                {
                                    active = true;
                                }

                            } // end if (jShift >= 0 && jShift <= 7)
                        } // end if (iShift >= 0 && iShift <= 7)
                    } // end if (Bearings[i])

                    Bearings[i] = active;
                    active = false;

                } // end for Bearing loop ----------------------------------------
            } // end for Range expansion loop ------------------------------------------

            return FoundInvader; // >>>>> RETURN >>>>>
        }

        public bool Arrange(sbyte[][] Coords = null)
        {
            bool success = false;

            return success;
        }

        public bool VerifyMove(chessman Claimant, int i1 = -128, int j1 = -128, int i2 = -128, int j2 = -128, bool enpassant = false)
        {
            bool verified = false;

            chessman KingPiece = PiecesArr[(int)Claimant.color][0];

            if (KingPiece != null)
            {
                if (i1 == -128 || j1 == -128)
                {
                    i1 = Claimant.posArr[0];
                    j1 = Claimant.posArr[1];
                }

                if (i2 == -128 || j2 == -128)
                {
                    i2 = i1;
                    j2 = j1;
                }

                // ------- Make the move -------------------------------------------------------------------------
                chessman Victim = CheckersField[i2, j2];
                
                if (enpassant || 
                    ( (Victim == null || !Victim.isAlive) && Claimant.piece == Pieces.Pawn && i1 != i2 )  )
                {
                    Victim = CheckersField[i2, j1];
                    CheckersField[i2, j1] = null;
                    enpassant = true;
                }

                CheckersField[i1, j1] = null;
                CheckersField[i2, j2] = Claimant;

                // ------- "under-shah" checking ------------------------------------------------------------------
                if (Require(KingPiece.posArr[0], KingPiece.posArr[1], Claimant.color) == null)
                {
                    verified = true;
                }

                // ------- UNDO -----------------------------------------------------------------------------------
                if (enpassant)
                {
                    CheckersField[i2, j1] = Victim;
                    CheckersField[i2, j2] = null;
                }
                else
                {
                    CheckersField[i2, j2] = Victim;
                }

                CheckersField[i1, j1] = Claimant;
            }
            else // there is no own King
            {
                verified = true;
            }
            
            return verified;
        }

        public sbyte Castling(Colors color, bool KingSide)
        {
            sbyte outCode = -128;

            chessman King = null;

            if (color == Colors.Black) King = BlackPieces[0];
            else King = WhitePieces[0];

            if (King != null)
            {
                sbyte i1 = King.posArr[0];
                sbyte j1 = King.posArr[1];

                if (KingSide) // ------------------------------------------------------
                {
                    if (King.tryCastling(1) == 1)
                    {
                        sbyte i2 = 6;
                        sbyte[,] movesArr = null;
                        sbyte[] NoteKing = new sbyte[8] { i1, j1, i2, j1, -1, 0, 0, 0 }; // { i1, j1, i2, j2, KS_cstl, 0, 0, 0 }

                        int Step = Scoresheet.StepNumber;

                        King.Move(Step, i2, j1, ref movesArr, free: true);
                        CheckersField[4, j1] = null;
                        CheckersField[6, j1] = King;

                        chessman cstlRook = CheckersField[7, j1];

                        cstlRook.Move(Step, 5, j1, ref movesArr, free: true);
                        CheckersField[7, j1] = null;
                        CheckersField[5, j1] = cstlRook;

                        Scoresheet.Write(NoteKing);

                        outCode = 5; // >>>>> KINGSIDE CASTLING
                    }
                    else outCode = -5; // >>>>> CASTLING IS PROHIBITED
                }
                else // Queenside -----------------------------------------------------
                {
                    if (King.tryCastling(2) == 2)
                    {
                        sbyte i2 = 2;
                        sbyte[,] movesArr = null;
                        sbyte[] NoteKing = new sbyte[8] { i1, j1, i2, j1, -2, 0, 0, 0 }; // { i1, j1, i2, j2, QS_cstl, 0, 0, 0 }

                        int Step = Scoresheet.StepNumber;

                        King.Move(Step, i2, j1, ref movesArr, free: true);
                        CheckersField[4, j1] = null;
                        CheckersField[2, j1] = King;

                        chessman cstlRook = CheckersField[0, j1];

                        cstlRook.Move(Step, 3, j1, ref movesArr, free: true);
                        CheckersField[0, j1] = null;
                        CheckersField[3, j1] = cstlRook;

                        Scoresheet.Write(NoteKing);

                        outCode = 4; // >>>>> QUEENSIDE CASTLING
                    }
                    else outCode = -5; // >>>>> CASTLING IS PROHIBITED
                }
            }

            return outCode;
        }

        public sbyte PlayerMove(sbyte [] vector, Colors color, ref sbyte[,] movesArr)
        {
            sbyte outCode = -128;
            sbyte i1 = vector[0];
            sbyte j1 = vector[1];
            
            if (i1 >= 0 && i1 <= 7 && j1 >= 0 && j1 <= 7)
            {
                chessman Owner = CheckersField[i1, j1];

                if (Owner != null && Owner.isAlive)
                {
                    if (Owner.color == color)
                    {
                        sbyte i2 = vector[2];
                        sbyte j2 = vector[3];
                        
                        if (i2 >= 0 && i2 <= 7 && j2 >= 0 && j2 <= 7)
                        {
                            if (Owner.piece == Pieces.King && Owner.InInitial && (i2 == 2 || i2 == 6) && (j2 == j1)) // then Player tries to castling
                            {
                                sbyte[,] movesArrRook = null; // perhaps it is not needed, cause cstlRook.Move is called with free: true

                                if (i2 == 2 && Owner.tryCastling(2) == 2) // then perform queenside castling
                                {
                                    int Step = Scoresheet.StepNumber;
                                    sbyte[] NoteKing = new sbyte[8] { i1, j1, i2, j1, -2, 0, 0, 0 }; // { i1, j1, i2, j2, QS_cstl, 0, 0, 0 }

                                    Owner.Move(Step, i2, j2, ref movesArr, free: true);
                                    CheckersField[4, j1] = null;
                                    CheckersField[2, j1] = Owner;
                                    
                                    chessman cstlRook = CheckersField[0, j1];

                                    cstlRook.Move(Step, 3, j2, ref movesArrRook, free: true);
                                    CheckersField[0, j1] = null;
                                    CheckersField[3, j1] = cstlRook;

                                    Scoresheet.Write(NoteKing);

                                    outCode = 4; // >>>>> QUEENSIDE CASTLING
                                }
                                else if (i2 == 6 && Owner.tryCastling(1) == 1) // then perform kingside castling
                                {
                                    int Step = Scoresheet.StepNumber;
                                    sbyte[] NoteKing = new sbyte[8] { i1, j1, i2, j1, -1, 0, 0, 0 }; // { i1, j1, i2, j2, KS_cstl, 0, 0, 0 }

                                    Owner.Move(Step, i2, j2, ref movesArr, free: true);
                                    CheckersField[4, j1] = null;
                                    CheckersField[6, j1] = Owner;
                                    
                                    chessman cstlRook = CheckersField[7, j1];

                                    cstlRook.Move(Step, 5, j2, ref movesArrRook, free: true);
                                    CheckersField[7, j1] = null;
                                    CheckersField[5, j1] = cstlRook;

                                    Scoresheet.Write(NoteKing);

                                    outCode = 5; // >>>>> KINGSIDE CASTLING
                                }
                                else outCode = -5; // >>>>> CASTLING IS PROHIBITED
                            }
                            else
                            {
                                int Step = Scoresheet.StepNumber;
                                sbyte done = Owner.Move(Step, i2, j2, ref movesArr);

                                if (done >= 0)
                                {
                                    chessman Victim = CheckersField[i2, j2];

                                    bool capture = false;
                                    bool enpassant = false;

                                    if (Victim != null && Victim.isAlive)
                                    {
                                        Victim.capture();
                                        capture = true;
                                    }
                                    else if (Owner.piece == Pieces.Pawn && i1 != i2)
                                    {
                                        Victim = CheckersField[i2, j1];
                                        Victim.capture();
                                        enpassant = true;
                                    }

                                    bool promotion = false;
                                    if (Owner.piece == Pieces.Pawn && (j2 == 0 || j2 == 7)) // then player has made promotion
                                    {
                                        promotion = true;
                                    }

                                    CheckersField[i1, j1] = null;
                                    CheckersField[i2, j2] = Owner;

                                    chessman KingPiece = PiecesArr[(int)color][0]; // !!!!!!! King is always the first and only !!!!!!! in array

                                    if (KingPiece != null) // checking of shah
                                    {
                                        chessman Invader = Require(KingPiece.posArr[0], KingPiece.posArr[1], color);

                                        if (Invader == null)
                                        {
                                            sbyte[] Note = new sbyte[8] { i1, j1, i2, j2, 0, 0, 0, 0 }; // { i1, j1, i2, j2, capture, 0, 0, 0 }

                                            if (capture || enpassant)
                                            {
                                                Note[4] = 1; 
                                                Note[5] = Victim.index; // { i1, j1, i2, j2, capture = 1, victim.index, 0, 0 }
                                            }

                                            if (promotion)
                                            {
                                                Note[4] += 2; 
                                                Note[6] = Owner.index; // { i1, j1, i2, j2, capture + 2 , victim.index, pawn.index, 0 } 
                                                Owner.capture(); // pawn will be replaced of any piece ; Note[7] will be filled in Chessboard.Addpiece()

                                                Scoresheet.StepDataTmp = Note;

                                                outCode = 3; // >>>>> PAWN PROMOTION AND PLAYER SHOULD SELECT ANY PIECE TO REPLACE
                                            }
                                            else
                                            {
                                                Scoresheet.Write(Note);

                                                outCode = 2; // >>>>> PLAYER HAS MADE THE MOVE
                                            }   
                                        }
                                        else // ---------------------- UNDO ----------------------------------------
                                        {
                                            movesArr[done, 0] -= 127; // mark move as "under shah"

                                            Owner.Move(Step, i1, j1, ref movesArr, free: true);
                                            CheckersField[i1, j1] = Owner;
                                            
                                            if (capture)
                                            {
                                                Victim.revive();
                                                CheckersField[i2, j2] = Victim;
                                            }
                                            else if (enpassant)
                                            {
                                                Victim.revive();
                                                CheckersField[i2, j2] = null;
                                            }
                                            else
                                            {
                                                CheckersField[i2, j2] = null;
                                            }

                                            outCode = -4; // >>>>> THE KING IN CHECK
                                        }
                                    }
                                    else // for debug and chess math
                                    {
                                        sbyte[] Note = new sbyte[8] { i1, j1, i2, j2, 0, 0, 0, 0 }; // { i1, j1, i2, j2, capture, 0, 0, 0 }

                                        if (capture)
                                        {
                                            Note[4] = 1;
                                            Note[5] = Victim.index; // { i1, j1, i2, j2, capture = 1, victim.index, 0, 0 }
                                        }

                                        if (promotion)
                                        {
                                            Note[4] += 2;
                                            Note[5] = Owner.index; // { i1, j1, i2, j2, capture + 2 , victim.index, pawn.index, 0 } 
                                            Owner.capture(); // pawn will be replaced of any piece ; Note[7] will be filled in Chessboard.Addpiece()

                                            Scoresheet.StepDataTmp = Note;

                                            outCode = 3; // >>>>> PAWN PROMOTION AND PLAYER SHOULD SELECT ANY PIECE TO REPLACE
                                        }
                                        else
                                        {
                                            Scoresheet.Write(Note);

                                            outCode = 1; // >>>>> PLAYER HAS MADE THE MOVE
                                        }

                                        Scoresheet.Write(Note);
                                    }
                                }
                                else outCode = -3; // >>>>> INVALID MOVE
                            }
                        } // end if (i2 j2)
                        else outCode = -2; // >>>>> COORDS OF THE SECOND CELL IS INVALID [BEYOND]
                    }
                    else outCode = -1; // >>>>> HANDS OFF MY CHESSMAN
                }
                else outCode = 0; // >>>>> COORDS OF THE FIRST CELL IS INVALID;    
            } // end if (i1 j1)
            else outCode = 0; // >>>>> COORDS OF THE FIRST CELL IS INVALID;


            if (outCode > 0) // then reset MovesArray
            {
                foreach ( chessman piece in BlackPieces)
                {
                    if (piece != null) piece.MovesArray = null;
                }

                foreach (chessman piece in WhitePieces)
                {
                    if (piece != null) piece.MovesArray = null;
                }
            }

            return outCode;
        }

        public void AIrecMove(int currStep, chessman Owner, sbyte i2, sbyte j2, bool RivalZero = false)
        {
            sbyte[] StepData = new sbyte[8];
            /* StepData = {
             *[0] i1, j1, i2, j2,           -- move vector
             *[4] -2 | -1 | 0 | 1 | 2 | 3   -- QS castling | KS castling | _ | capture | promotion | capture + promotion
             *[5] 0 ... 23                  -- index of the captured piece
             *[6] 0 ... 23                  -- index of the promoted pawn
             *[7] 0 ...  4                  -- enum of the replaced piece, 0 - no promotion  
             * } */

            sbyte i1 = Owner.posArr[0];
            sbyte j1 = Owner.posArr[1];

            StepData[0] = i1;
            StepData[1] = j1;
            StepData[2] = i2;
            StepData[3] = j2;
            StepData[4] = 0;

            if (Owner.piece == Pieces.King && Owner.InInitial)
            {
                if (i2 == 6) // ----- Kingside castling -----
                {
                    chessman RookKS = CheckersField[7, j2];

                    Owner.AIMove(currStep, 6, j2);
                    CheckersField[4, j2] = null;
                    CheckersField[6, j2] = Owner;

                    RookKS.AIMove(currStep, 5, j2);
                    CheckersField[7, j2] = null;
                    CheckersField[5, j2] = RookKS;

                    StepData[4] = -1;

                    return; // >>>>> OUT >>>>>
                }
                else if (i2 == 2) // ----- Queenside castling -----
                {
                    chessman RookQS = CheckersField[0, j2];

                    Owner.AIMove(currStep, 2, j2);
                    CheckersField[4, j2] = null;
                    CheckersField[2, j2] = Owner;

                    RookQS.AIMove(currStep, 3, j2);
                    CheckersField[0, j2] = null;
                    CheckersField[3, j2] = RookQS;

                    StepData[4] = -2;

                    return; // >>>>> OUT >>>>>
                }
            } // end if (Owner.piece == Pieces.King && Owner.InInitial)

            chessman Victim = CheckersField[i2, j2];

            if (Victim != null && Victim.isAlive)
            {
                StepData[4] = 1;
                StepData[5] = Victim.index;
                Victim.capture();
            }
            else if (Owner.piece == Pieces.Pawn && i1 != i2) // en passant
            {
                Victim = CheckersField[i2, j1];
                Victim.capture();

                StepData[4] = 1;
                StepData[5] = Victim.index;

                CheckersField[i2, j1] = null;
            }

            Owner.AIMove(currStep, i2, j2);
            CheckersField[i1, j1] = null;
            CheckersField[i2, j2] = Owner;

            if (Owner.piece == Pieces.Pawn && (j2 == 0 || j2 == 7)) // then AI has made promotion
            {
                StepData[4] += 2;
                StepData[6] = Owner.index;

                Addpiece(Owner.color, Pieces.Queen, i2, j2, true);

                StepData[7] = (sbyte)Pieces.Queen;
            }

            Scoresheet.Write(StepData, RivalZero: RivalZero);

            return; // >>>>> OUT >>>>>
        }

        public void AItrialMove(sbyte[] StepData, int currStep, chessman Owner, sbyte i2, sbyte j2)
        {
            /* StepData = {
             *[0] i1, j1, i2, j2,           -- move vector
             *[4] -2 | -1 | 0 | 1 | 2 | 3   -- QS castling | KS castling | _ | capture | promotion | capture + promotion
             *[5] 0 ... 23                  -- index of the captured piece
             *[6] 0 ... 23                  -- index of the promoted pawn 
             * } */
             
            sbyte i1 = Owner.posArr[0];
            sbyte j1 = Owner.posArr[1];

            StepData[0] = i1;
            StepData[1] = j1;
            StepData[2] = i2;
            StepData[3] = j2;
            StepData[4] = 0;

            if (Owner.piece == Pieces.King && Owner.InInitial)
            {
                if (i2 == 6) // ----- Kingside castling -----
                {
                    chessman RookKS = CheckersField[7, j2];

                    Owner.AIMove(currStep, 6, j2);
                    CheckersField[4, j2] = null;
                    CheckersField[6, j2] = Owner;

                    RookKS.AIMove(currStep, 5, j2);
                    CheckersField[7, j2] = null;
                    CheckersField[5, j2] = RookKS;

                    StepData[4] = -1;

                    return; // >>>>> OUT >>>>>
                }
                else if (i2 == 2) // ----- Queenside castling -----
                {
                    chessman RookQS = CheckersField[0, j2];

                    Owner.AIMove(currStep, 2, j2);
                    CheckersField[4, j2] = null;
                    CheckersField[2, j2] = Owner;

                    RookQS.AIMove(currStep, 3, j2);
                    CheckersField[0, j2] = null;
                    CheckersField[3, j2] = RookQS;

                    StepData[4] = -2;

                    return; // >>>>> OUT >>>>>
                }
            } // end if (Owner.piece == Pieces.King && Owner.InInitial)

            chessman Victim = CheckersField[i2, j2];

            if (Victim != null && Victim.isAlive)
            {
                StepData[4] = 1;
                StepData[5] = Victim.index;
                Victim.capture();
            }
            else if (Owner.piece == Pieces.Pawn && i1 != i2) // en passant
            {
                Victim = CheckersField[i2, j1];
                Victim.capture();

                StepData[4] = 1;
                StepData[5] = Victim.index;

                CheckersField[i2, j1] = null;
            }

            Owner.AIMove(currStep, i2, j2);
            CheckersField[i1, j1] = null;
            CheckersField[i2, j2] = Owner;

            if (Owner.piece == Pieces.Pawn && (j2 == 0 || j2 == 7)) // then AI has made promotion
            {
                StepData[4] += 2;
                StepData[6] = Owner.index;

                Addpiece(Owner.color, Pieces.Queen, i2, j2, true);
            }

            return; // >>>>> OUT >>>>>
        }

        /*
        public bool AItrialMove(sbyte[] StepData, int currStep, int colorInd, int pieceInd, sbyte i2, sbyte j2)
        {
            /* StepData = {
             *[0] i1, j1, i2, j2,           -- move vector
             *[4] -2 | -1 | 0 | 1 | 2 | 3   -- QS castling | KS castling | _ | capture | promotion | capture + promotion
             *[5] 0 ... 23                  -- index of the captured piece
             *[6] 0 ... 23                  -- index of the promoted pawn 
             * }

            chessman Owner = PiecesArr[colorInd][pieceInd];

            if (Owner == null || !Owner.isAlive) return false; // >>>>> OUT >>>>>

            sbyte i1 = Owner.posArr[0];
            sbyte j1 = Owner.posArr[1];

            StepData[0] = i1;
            StepData[1] = j1;
            StepData[2] = i2;
            StepData[3] = j2;

            if (Owner.piece == Pieces.King && Owner.InInitial)
            {
                if (i2 == 6) // ----- Kingside castling -----
                {
                    chessman RookKS = CheckersField[7, j2];

                    Owner.AIMove(currStep, 6, j2);
                    CheckersField[4, j2] = null;
                    CheckersField[6, j2] = Owner;

                    RookKS.AIMove(currStep, 5, j2);
                    CheckersField[7, j2] = null;
                    CheckersField[5, j2] = RookKS;

                    StepData[4] = -1;

                    return true; // >>>>> OUT >>>>>
                }
                else if (i2 == 2) // ----- Queenside castling -----
                {
                    chessman RookQS = CheckersField[0, j2];

                    Owner.AIMove(currStep, 2, j2);
                    CheckersField[4, j2] = null;
                    CheckersField[2, j2] = Owner;

                    RookQS.AIMove(currStep, 3, j2);
                    CheckersField[0, j2] = null;
                    CheckersField[3, j2] = RookQS;

                    StepData[4] = -2;

                    return true; // >>>>> OUT >>>>>
                }
            } // end if (Owner.piece == Pieces.King && Owner.InInitial)

            chessman Victim = CheckersField[i2, j2];

            if (Victim != null && Victim.isAlive)
            {
                StepData[4] = 1;
                StepData[5] = Victim.index;
            }
            else if (Owner.piece == Pieces.Pawn && i1 != i2) // en passant
            {
                Victim = CheckersField[i2, j1];

                StepData[4] = 1;
                StepData[5] = Victim.index;

                CheckersField[i2, j1] = null;
            }

            Owner.AIMove(currStep, i2, j2);
            CheckersField[i1, j1] = null;
            CheckersField[i2, j2] = Owner;

            if (Owner.piece == Pieces.Pawn && (j2 == 0 || j2 == 7)) // then AI has made promotion
            {
                StepData[4] += 2;
                StepData[6] = Owner.index;

                Addpiece((Colors)colorInd, Pieces.Queen, i2, j2, true);
            }
            
            return true; // >>>>> OUT >>>>>
        } */

        public void AIundoMove(sbyte[] StepData, int stepInd)
        {
            sbyte i1 = StepData[0];
            sbyte j1 = StepData[1];
            sbyte i2 = StepData[2];
            sbyte j2 = StepData[3];
            sbyte eventInd = StepData[4];

            chessman Owner = CheckersField[i2, j2];

            switch (eventInd)
            {
                case 0:
                    Owner.AIMove(stepInd, i1, j1);

                    CheckersField[i2, j2] = null;
                    CheckersField[i1, j1] = Owner;

                    break;

                case 1: // capture
                    chessman Victim;

                    if (Owner.color == Colors.Black) Victim = WhitePieces[StepData[5]];
                    else Victim = BlackPieces[StepData[5]];

                    Owner.AIMove(stepInd, i1, j1);

                    CheckersField[i1, j1] = Owner;
                    CheckersField[i2, j2] = null;

                    CheckersField[Victim.posArr[0], Victim.posArr[1]] = Victim;
                    Victim.revive();

                    break;

                case 2: // promotion
                    //Owner = CheckersField[i2, j2]; // converted piece

                    if (Owner.color == Colors.Black)
                    {
                        PiecesArr[0][Owner.index] = null;

                        Owner = BlackPieces[StepData[6]];    
                    }
                    else
                    {
                        PiecesArr[1][Owner.index] = null;

                        Owner = WhitePieces[StepData[6]];
                    }

                    Owner.revive();
                    Owner.AIMove(stepInd, i1, j1);

                    CheckersField[i2, j2] = null;
                    CheckersField[i1, j1] = Owner;

                    break;

                case 3: // capture + promotion
                    //Owner = CheckersField[i2, j2]; // converted piece

                    chessman Victim3;

                    if (Owner.color == Colors.Black)
                    {
                        PiecesArr[0][Owner.index] = null;

                        Owner = BlackPieces[StepData[6]];
                        Victim3 = WhitePieces[StepData[5]];
                    }
                    else
                    {
                        PiecesArr[1][Owner.index] = null;

                        Owner = WhitePieces[StepData[6]];
                        Victim3 = BlackPieces[StepData[5]];
                    }

                    Owner.revive();
                    Owner.AIMove(stepInd, i1, j1);

                    CheckersField[i2, j2] = Victim3;
                    CheckersField[i1, j1] = Owner;

                    Victim3.revive();

                    break;

                case -1: // Kingside castling
                    Owner.AIMove(stepInd, 4, j1);

                    CheckersField[6, j1] = null;
                    CheckersField[4, j1] = Owner;

                    chessman RookKS = CheckersField[5, j1];

                    RookKS.AIMove(stepInd, 7, j1);

                    CheckersField[5, j1] = null;
                    CheckersField[7, j1] = RookKS;

                    break;

                case -2: // Queenside castling
                    Owner.AIMove(stepInd, 4, j1);

                    CheckersField[2, j1] = null;
                    CheckersField[4, j1] = Owner;

                    chessman RookQS = CheckersField[3, j1];

                    RookQS.AIMove(stepInd, 0, j1);

                    CheckersField[3, j1] = null;
                    CheckersField[0, j1] = RookQS;

                    break;
            }    
        }

        public void UndoMove(sbyte[] StepData, int stepInd)
        {
            sbyte i1 = StepData[0];
            sbyte j1 = StepData[1];
            sbyte i2 = StepData[2];
            sbyte j2 = StepData[3];
            sbyte eventInd = StepData[4];

            chessman Owner = CheckersField[i2, j2];

            switch (eventInd)
            {
                case 0:
                    Owner.AIMove(stepInd, i1, j1);

                    CheckersField[i2, j2] = null;
                    CheckersField[i1, j1] = Owner;

                    break;

                case 1: // capture
                    chessman Victim;

                    if (Owner.color == Colors.Black) Victim = WhitePieces[StepData[5]];
                    else Victim = BlackPieces[StepData[5]];

                    Owner.AIMove(stepInd, i1, j1);

                    CheckersField[i1, j1] = Owner;
                    CheckersField[i2, j2] = null;

                    CheckersField[Victim.posArr[0], Victim.posArr[1]] = Victim;
                    Victim.revive();

                    break;

                case 2: // promotion
                    //Owner = CheckersField[i2, j2]; // converted piece

                    if (Owner.color == Colors.Black)
                    {
                        PiecesArr[0][Owner.index] = null;

                        Owner = BlackPieces[StepData[6]];
                    }
                    else
                    {
                        PiecesArr[1][Owner.index] = null;

                        Owner = WhitePieces[StepData[6]];
                    }

                    Owner.revive();
                    Owner.AIMove(stepInd, i1, j1);

                    CheckersField[i2, j2] = null;
                    CheckersField[i1, j1] = Owner;

                    break;

                case 3: // capture + promotion
                    //Owner = CheckersField[i2, j2]; // converted piece

                    chessman Victim3;

                    if (Owner.color == Colors.Black)
                    {
                        PiecesArr[0][Owner.index] = null;

                        Owner = BlackPieces[StepData[6]];
                        Victim3 = WhitePieces[StepData[5]];
                    }
                    else
                    {
                        PiecesArr[1][Owner.index] = null;

                        Owner = WhitePieces[StepData[6]];
                        Victim3 = BlackPieces[StepData[5]];
                    }

                    Owner.revive();
                    Owner.AIMove(stepInd, i1, j1);

                    CheckersField[i2, j2] = Victim3;
                    CheckersField[i1, j1] = Owner;

                    Victim3.revive();

                    break;

                case -1: // Kingside castling
                    Owner.AIMove(stepInd, 4, j1);

                    CheckersField[6, j1] = null;
                    CheckersField[4, j1] = Owner;

                    chessman RookKS = CheckersField[5, j1];

                    RookKS.AIMove(stepInd, 7, j1);

                    CheckersField[5, j1] = null;
                    CheckersField[7, j1] = RookKS;

                    break;

                case -2: // Queenside castling
                    Owner.AIMove(stepInd, 4, j1);

                    CheckersField[2, j1] = null;
                    CheckersField[4, j1] = Owner;

                    chessman RookQS = CheckersField[3, j1];

                    RookQS.AIMove(stepInd, 0, j1);

                    CheckersField[3, j1] = null;
                    CheckersField[0, j1] = RookQS;

                    break;
            }
        }

        public void RedoMove(sbyte[] StepData, int stepInd)
        {
            sbyte i1 = StepData[0];
            sbyte j1 = StepData[1];
            sbyte i2 = StepData[2];
            sbyte j2 = StepData[3];
            sbyte eventInd = StepData[4];

            chessman Owner = CheckersField[i1, j1];

            switch (eventInd)
            {
                case 0:
                    Owner.AIMove(stepInd, i2, j2);

                    CheckersField[i2, j2] = Owner;
                    CheckersField[i1, j1] = null;

                    break;

                case 1: // capture
                    chessman Victim;

                    if (Owner.color == Colors.Black) Victim = WhitePieces[StepData[5]];
                    else Victim = BlackPieces[StepData[5]];

                    CheckersField[Victim.posArr[0], Victim.posArr[1]] = null;
                    Victim.capture();

                    Owner.AIMove(stepInd, i2, j2);

                    CheckersField[i1, j1] = null;
                    CheckersField[i2, j2] = Owner;
                    
                    break;

                case 2: // promotion
                    Owner.AIMove(stepInd, i2, j2);
                    Owner.capture();

                    CheckersField[i1, j1] = null;

                    Addpiece(Owner.color, (Pieces)StepData[7], i2, j2, true);
                    
                    break;

                case 3: // capture + promotion
                    chessman Victim3 = CheckersField[i2, j2];
                    Victim3.capture();

                    Owner.AIMove(stepInd, i2, j2);
                    Owner.capture();

                    CheckersField[i1, j1] = null;

                    Addpiece(Owner.color, (Pieces)StepData[7], i2, j2, true);

                    break;

                case -1: // Kingside castling
                    chessman RookKS = CheckersField[7, j2];

                    Owner.AIMove(stepInd, 6, j2);
                    CheckersField[4, j2] = null;
                    CheckersField[6, j2] = Owner;

                    RookKS.AIMove(stepInd, 5, j2);
                    CheckersField[7, j2] = null;
                    CheckersField[5, j2] = RookKS;

                    break;

                case -2: // Queenside castling
                    chessman RookQS = CheckersField[0, j2];

                    Owner.AIMove(stepInd, 2, j2);
                    CheckersField[4, j2] = null;
                    CheckersField[2, j2] = Owner;

                    RookQS.AIMove(stepInd, 3, j2);
                    CheckersField[0, j2] = null;
                    CheckersField[3, j2] = RookQS;

                    break;
            }
        }

        public sbyte Addpiece(Colors color, Pieces piece, sbyte ifile = 0, sbyte jrank = 0, bool promotion = false)
        {
            sbyte outCode = -1;
            int indColor = 0;
            int indStart = 1; // 0 index is reserved for King

            if (color != Colors.nil)
            {
                indColor = (int)color;

                if (piece == Pieces.King)
                {
                    chessman chKing = PiecesArr[indColor][0];

                    if (chKing != null)
                    {
                        CheckersField[ chKing.posArr[0], chKing.posArr[1] ] = null;
                    }

                    outCode = 0;
                }
                else
                {
                    outCode = -127; // >>>>> FULL SET >>>>>

                    if (promotion) indStart = 16;

                    for (int i = indStart; i < PiecesArr[indColor].GetLength(0); i++)
                    {
                        if (PiecesArr[indColor][i] == null)
                        {
                            outCode = (sbyte)i;
                            break;
                        }
                    }    
                }
            }
            else outCode = -128; // >>>>> COLOR IS NOT SPECIFIED >>>>>

            if (outCode >= 0)
            {
                if (ifile >= 0 && ifile <= 7 && jrank >= 0 && jrank <= 7)
                {
                    chessman Owner = CheckersField[ifile, jrank];

                    if (Owner != null)
                    {
                        if (promotion)
                        {
                            Owner.capture(); // the Owner is a promoted pawn
                        }
                        else
                        {
                            PiecesArr[indColor][Owner.index] = null;
                        }   
                    }

                    switch(piece)
                    {
                        case Pieces.King:
                            if (ifile == King.IniPosition[indColor][0] && jrank == King.IniPosition[indColor][1])
                            {
                                PiecesArr[indColor][0] = new King(this, 0, color);
                            }
                            else PiecesArr[indColor][0] = new King(this, 0, color, new sbyte[2] {ifile, jrank});

                            break;

                        case Pieces.Queen:
                            PiecesArr[indColor][outCode] = new Queen(this, outCode, color, new sbyte[2] { ifile, jrank });
                            break;

                        case Pieces.Rook:
                            if ((ifile == Rook.IniPosition[indColor][0, 0] && jrank == Rook.IniPosition[indColor][0, 1]) ||
                                (ifile == Rook.IniPosition[indColor][1, 0] && jrank == Rook.IniPosition[indColor][1, 1]) )
                            {
                                PiecesArr[indColor][outCode] = new Rook(this, outCode, color);
                            }
                            else PiecesArr[indColor][outCode] = new Rook(this, outCode, color, new sbyte[2] { ifile, jrank });

                            break;

                        case Pieces.Bishop:
                            PiecesArr[indColor][outCode] = new Bishop(this, outCode, color, new sbyte[2] { ifile, jrank });
                            break;

                        case Pieces.Knight:
                            PiecesArr[indColor][outCode] = new Knight(this, outCode, color, new sbyte[2] { ifile, jrank });
                            break;

                        case Pieces.Pawn:
                            if (jrank == Pawn.IniPosition[indColor])
                            {
                                PiecesArr[indColor][outCode] = new Pawn(this, outCode, color);
                            }
                            else PiecesArr[indColor][outCode] = new Pawn(this, outCode, color, new sbyte[2] { ifile, jrank });

                            break;
                    }

                    CheckersField[ifile, jrank] = PiecesArr[indColor][outCode];

                }
                else outCode = -126; // >>>>> INVALID COORDS >>>>>
            }

            return outCode;
        }

        public sbyte Deletepiece(int ifile, int jrank)
        {
            sbyte outCode = -1;

            if (ifile >= 0 && ifile <= 7 && jrank >= 0 && jrank <= 7)
            {
                chessman Owner = CheckersField[ifile, jrank];

                if (Owner != null)
                {
                    PiecesArr[(int)Owner.color][Owner.index] = null;
                    CheckersField[ifile, jrank] = null;
                    outCode = Owner.index;
                }
            }
            else outCode = -126; // >>>>> INVALID COORDS >>>>>

            return outCode;
        }

        public Chessboard(bool desert = false, string name = "New_chessboard")
        {
            this.Name = name;
            Scoresheet = new Journal(this);

            if (!desert) // then make standard initial placement of pieces 
            {
                sbyte ifile, jrank;

                for (sbyte i = 0; i < 16; i++)
                {
                    switch (i)
                    {
                        case 0: // King
                            PiecesArr[0][0] = new King(this, 0, color: Colors.Black);
                            PiecesArr[1][0] = new King(this, 0, color: Colors.White);

                            CheckersField[4, 7] = PiecesArr[0][0];
                            CheckersField[4, 0] = PiecesArr[1][0];

                            break;

                        case 1: // Queen
                            PiecesArr[0][1] = new Queen(this, 1, color: Colors.Black);
                            PiecesArr[1][1] = new Queen(this, 1, color: Colors.White);

                            CheckersField[3, 7] = PiecesArr[0][1];
                            CheckersField[3, 0] = PiecesArr[1][1];

                            break;
                        case 2: // Rook L
                            PiecesArr[0][2] = new Rook(this, 2, color: Colors.Black);
                            PiecesArr[1][2] = new Rook(this, 2, color: Colors.White);

                            CheckersField[0, 7] = PiecesArr[0][2];
                            CheckersField[0, 0] = PiecesArr[1][2];

                            break;
                        case 3: // Rook R
                            PiecesArr[0][3] = new Rook(this, 3, color: Colors.Black);
                            PiecesArr[1][3] = new Rook(this, 3, color: Colors.White);

                            CheckersField[7, 7] = PiecesArr[0][3];
                            CheckersField[7, 0] = PiecesArr[1][3];

                            break;
                        case 4: // Bishop L
                            PiecesArr[0][4] = new Bishop(this, 4, color: Colors.Black);
                            PiecesArr[1][4] = new Bishop(this, 4, color: Colors.White);

                            CheckersField[2, 7] = PiecesArr[0][4];
                            CheckersField[2, 0] = PiecesArr[1][4];

                            break;
                        case 5: // Bishop R
                            PiecesArr[0][5] = new Bishop(this, 5, color: Colors.Black);
                            PiecesArr[1][5] = new Bishop(this, 5, color: Colors.White);

                            CheckersField[5, 7] = PiecesArr[0][5];
                            CheckersField[5, 0] = PiecesArr[1][5];

                            break;
                        case 6: // Knight L
                            PiecesArr[0][6] = new Knight(this, 6, color: Colors.Black);
                            PiecesArr[1][6] = new Knight(this, 6, color: Colors.White);

                            CheckersField[1, 7] = PiecesArr[0][6];
                            CheckersField[1, 0] = PiecesArr[1][6];

                            break;
                        case 7: // Knight R
                            PiecesArr[0][7] = new Knight(this, 7, color: Colors.Black);
                            PiecesArr[1][7] = new Knight(this, 7, color: Colors.White);

                            CheckersField[6, 7] = PiecesArr[0][7];
                            CheckersField[6, 0] = PiecesArr[1][7];

                            break;
                        default: // Pawns
                            PiecesArr[0][i] = new Pawn(this, i, color: Colors.Black);
                            PiecesArr[1][i] = new Pawn(this, i, color: Colors.White);

                            CheckersField[i - 8, 6] = PiecesArr[0][i];
                            CheckersField[i - 8, 1] = PiecesArr[1][i];

                            break;
                    }
                } // end for (byte i = 0; i < 16; i++)
            }
            
        }
    } // end of public class Chessboard ///////////////////////////////////////////////////////////////////////

    public abstract class ChessAI
    {
        protected Chessboard Board;
        protected chessman[] Teammates;
        protected chessman[] Rivals;
        
        protected Colors Coalition;
        protected Colors RivalColor;
        protected byte ColorIndex;
        protected byte RivalIndex;

        public string StepString = "";

        public virtual string Forecast { get; set; }
        public virtual string infoDesc { get { return ""; } }
        public virtual string infoBlack { get; set; }
        public virtual string infoWhite { get; set; }
        
        public virtual chessman Testee { get; set; }

        public virtual void RestoreData() { }

        public abstract int MakeMove();

    } // end of public abstract class ChessAI //////////////////////////////////////////////////////////////////

    public class Sally : ChessAI
    {
        private sbyte[][,] Arsenal = new sbyte[24][,];
        private byte[][] MovesData = new byte[137][];
        // MovesData[i, ] = { pieceInd, moveInd, NumRival, CapRival, minNum, minCap, maxNum, maxCap, maxThreat, minThreat } 

        private byte totNumTeam = 20;
        private byte totNumRival = 20;
        private byte OwnCapacity = 0; // 137 for standard set

        private string forecastStr = "";

        public override string Forecast
        {
            get { return forecastStr; }    
        }

        public override string infoDesc
        {
            get
            {
                return "W's and B's mobility";
            }
        }

        public override string infoBlack
        {
            get
            {
                if (Coalition == Colors.Black)
                {
                    return String.Format("{0,3}", totNumTeam);
                }
                else
                {
                    return String.Format("{0,3}", totNumRival);
                }
            }
        }

        public override string infoWhite
        {
            get
            {
                if (Coalition == Colors.White)
                {
                    return String.Format("{0,3}", totNumTeam);
                }
                else
                {
                    return String.Format("{0,3}", totNumRival);
                }
            }
        }

        public static byte[] SortAsc(byte[] PieceValue)
        {
            byte tmp = 0;
            int Num = PieceValue.GetLength(0);

            if (Num > 1)
            {
                for (int ind = 1; ind < Num; ind++)
                {
                    tmp = PieceValue[ind];

                    for (int j = ind - 1; j >= 0; j--)
                    {
                        if (tmp < PieceValue[j])
                        {
                            PieceValue[j + 1] = PieceValue[j];
                        }
                        else
                        {
                            PieceValue[j + 1] = tmp;
                            break;
                        }

                        PieceValue[j] = tmp;
                    }
                }
            }
            
            return PieceValue;
        }

        public bool Offensive(chessman Owner) // in simplified manner ; rival move is next
        {
            bool attacked = false; // if attacked is false then Owner is defended

            // it is assumed that owner is not the King (see DefineMoves() for the King class)
            sbyte ifile = Owner.posArr[0];
            sbyte jrank = Owner.posArr[1];

            chessman[] Invaders = new chessman[16];

            if (Board.Require(ifile, jrank, Coalition, Invaders: Invaders) != null)
            {
                attacked = true;

                chessman[] Defenders = new chessman[16];

                if (Board.Require(ifile, jrank, RivalColor, Invaders: Defenders) != null)
                {
                    byte InvNum = 0 , DefNum = 0;
                    byte InvTot = 0, DefTot = 0;

                    byte[] InvValue = new byte[16];
                    byte[] DefValue = new byte[16];

                    bool InvKing = false, DefKing = false;

                    chessman invader, defender;

                    for (int i = 0; i < 16; i++)
                    {
                        invader = Invaders[i];
                        defender = Defenders[i];

                        if (invader != null)
                        {
                            if (invader.piece == Pieces.King)
                            {
                                InvKing = true;
                            }
                            else
                            {
                                InvValue[InvNum] = invader.MaxMoves;

                                InvTot += invader.MaxMoves;

                                InvNum++;
                            }    
                        }

                        if (defender != null)
                        {
                            if (defender.piece == Pieces.King)
                            {
                                DefKing = true;
                            }
                            else
                            {
                                DefValue[DefNum] = defender.MaxMoves;

                                DefTot += defender.MaxMoves;

                                DefNum++;
                            }    
                        }
                    } // end for (int i = 0; i < 16; i++)

                    byte InvLoss = 0, DefLoss = 0;

                    int NumDiff = InvNum - DefNum;

                    if (!InvKing && DefKing && NumDiff <= 1) DefNum++;
                    else if (!DefKing && InvKing && NumDiff >= 0) InvNum++;

                    NumDiff = InvNum - DefNum;

                    if (NumDiff >= 1)
                    {
                        DefLoss = Owner.MaxMoves;
                        DefLoss += DefTot;

                        SortAsc(InvValue);

                        for (int i = 0; i < DefNum; i++) InvLoss += InvValue[i];
                    }
                    else
                    {
                        InvLoss = InvTot;

                        SortAsc(DefValue);

                        if (InvNum > 0)
                        {
                            InvNum--;

                            DefLoss = Owner.MaxMoves;
                            for (int i = 0; i < InvNum; i++) DefLoss += DefValue[i];
                        }
                        else
                        {
                            InvNum = 0;
                            DefLoss = 0;
                        }
                    }

                    if (InvLoss >= DefLoss) attacked = false;
                }
            }

            return attacked;
        }

        public bool Defense(chessman Owner, sbyte[,] movesArr, byte Num, int currStep, bool OwnTurn = true) // in simplified manner ; own move is next
        {
            bool defended = true; // if defended is true then Owner is defended

            sbyte ifile = Owner.posArr[0];
            sbyte jrank = Owner.posArr[1];

            chessman Invader = Board.Require(ifile, jrank, Owner.color);

            if (Invader != null)
            {
                defended = false;

                if (Owner.piece != Pieces.King)
                {
                    Invader.capture(); // to prevent occlusion

                    chessman Defender = Board.Require(ifile, jrank, Invader.color);

                    Invader.revive();

                    if (Defender != null && Owner.MaxMoves <= Invader.MaxMoves)
                    {
                        defended = true;
                    }
                    else if (OwnTurn) // threat reality check 
                    {
                        sbyte i2 = Invader.posArr[0];
                        sbyte j2 = Invader.posArr[1];
                        bool proved = false;

                        for (int ind = 0; ind < Num; ind++)
                        {
                            if (movesArr[ind, 0] == i2 && movesArr[ind, 1] == j2)
                            {
                                proved = true;
                                break;
                            }
                        }

                        if (proved)
                        {
                            sbyte[] StepData = new sbyte[7];

                            Board.AItrialMove(StepData, currStep, Owner, i2, j2); // --- TRIAL MOVE L2 ---
                            // ------- LEVEL 3 -----------------------------------------------------------

                            chessman RivalDef = Board.Require(i2, j2, Owner.color);

                            if (RivalDef == null)
                            {
                                defended = true;
                            }
                            else
                            {
                                movesArr = RivalDef.DefineMoves(out Num, currStep + 1, true);

                                proved = false;

                                for (int ind = 0; ind < Num; ind++)
                                {
                                    if (movesArr[ind, 0] == i2 && movesArr[ind, 1] == j2)
                                    {
                                        proved = true;
                                        break;
                                    }
                                }

                                if (proved)
                                {
                                    if (Owner.MaxMoves <= Invader.MaxMoves)
                                    {
                                        defended = true;
                                    }
                                }
                                else
                                {
                                    defended = true;
                                }
                            }

                            Board.AIundoMove(StepData, currStep); // --- UNDO RIVAL MOVE -------------------
                            // ------- LEVEL 2--------------------------------------------------------------
                        }
                    }
                }    
            }

            return defended;
        }

        public override int MakeMove()
        {
            int OutCode = 0; // if OutCode >= 0 then put its value to Equivalents
            forecastStr = "";
            string criterionStr = "";
            string currCrit = "";

            sbyte[,] movesArr;
            sbyte[,] movesArrRiv;
            sbyte[,] movesArrL2;

            totNumTeam = 0;

            byte Num, totNumL2, totCapL2, NumL2;
            byte totCapRival, NumRiv;

            byte currCapacity = 0, IniCapRival = 0;

            for (int i = 0; i < Teammates.GetLength(0); i++)
            {
                if (Teammates[i] != null && Teammates[i].isAlive) currCapacity += Teammates[i].MaxMoves;

                if (Rivals[i] != null && Rivals[i].isAlive) IniCapRival += Rivals[i].MaxMoves; 
            }

            bool exchangeLogic = (currCapacity < OwnCapacity);
            int priorLoss = OwnCapacity - currCapacity;

            OwnCapacity = currCapacity;

            int currStep = Board.Scoresheet.StepNumber;
            int nextStep = currStep + 1;
            int lastStep = currStep + 2;

            sbyte[] currStepData = new sbyte[7];
            sbyte[] nextStepData = new sbyte[7];

            int index = 0;
            byte minNum, minCap, maxNum, maxCap, maxThreat, minThreat, threatNum, attackNum;

            // DataPly = { pieceInd, moveInd, NumRival, CapRival, minNum, minCap, maxNum, maxCap, maxThreat, minThreat, attackNum } 
            MovesData[0] = new byte[11] { 0, 0, 255, 255, 0, 0, 0, 0, 255, 0, 0 };

            int[] LossDiff = new int[137]; // RivLoss - TeamLoss
            LossDiff[0] = -1000;
            int currDiff = 0, LossRival = 0;

            byte[] DataPly;
            bool replaceFlag = false;
            bool firstEntry = true;

            chessman PieceOwnL0, PieceOwnL1, PieceRivL1, PieceOwnL2;
            bool[] DefenseStatus = new bool[24];

            for (byte pieceInd = 0; pieceInd < 24; pieceInd++) // --- TRAVERSE OF OWN PIECES ----------------
            {
                PieceOwnL0 = Teammates[pieceInd];

                if (PieceOwnL0 != null && PieceOwnL0.isAlive)
                {
                    movesArr = PieceOwnL0.DefineMoves(out Num, currStep, true);
                    Arsenal[pieceInd] = movesArr;

                    totNumTeam += Num;

                    for (byte moveInd = 0; moveInd < Num; moveInd++) // --- TRAVERSE OF OWN MOVES ------------
                    {
                        Board.AItrialMove(currStepData, currStep, PieceOwnL0, movesArr[moveInd, 0], movesArr[moveInd, 1]); // --- TRIAL MOVE L0 ---
                        // ------- LEVEL 1 --------------------------------------------------------------------------------------------------------

                        totNumRival = 0;
                        totCapRival = 0;
                        minNum = 255; // byte.MaxValue;
                        minCap = 255; // byte.MaxValue;
                        maxNum = 0;
                        maxCap = 0;
                        maxThreat = 0;
                        minThreat = 255; // byte.MaxValue;
                        attackNum = 0;

                        for (byte pieceIndDEF = 0; pieceIndDEF < 24; pieceIndDEF++) // CHECKING DEFENSE ---
                        {
                            DefenseStatus[pieceIndDEF] = false;

                            PieceOwnL1 = Teammates[pieceIndDEF];

                            if (PieceOwnL1 != null && PieceOwnL1.isAlive)
                            {
                                if (!Offensive(PieceOwnL1))
                                {
                                    DefenseStatus[pieceIndDEF] = true;
                                }
                            }
                        } // end of CHECKING DEFENSE ----------------------------------------

                        for (byte piRivInd = 0; piRivInd < 24; piRivInd++) // --- TRAVERSE OF RIVAL PIECES -----
                        {
                            PieceRivL1 = Rivals[piRivInd];

                            if (PieceRivL1 != null && PieceRivL1.isAlive)
                            {
                                movesArrRiv = PieceRivL1.DefineMoves(out NumRiv, nextStep, true);

                                totNumRival += NumRiv;
                                totCapRival += PieceRivL1.MaxMoves;

                                if (!Defense(PieceRivL1, movesArrRiv, NumRiv, nextStep))
                                {
                                    attackNum++;
                                }

                                for (byte mvRivInd = 0; mvRivInd < NumRiv; mvRivInd++) // --- TRAVERSE OF RIVAL MOVES -----
                                {
                                    Board.AItrialMove(nextStepData, nextStep, PieceRivL1, movesArrRiv[mvRivInd, 0], movesArrRiv[mvRivInd, 1]); // --- TRIAL MOVE L1 ---
                                    // ------- LEVEL 2 ----------------------------------------------------------------------------------------------------------------

                                    totNumL2 = 0;
                                    totCapL2 = 0;
                                    threatNum = 0;

                                    for (byte pieceIndL2 = 0; pieceIndL2 < 24; pieceIndL2++)
                                    {
                                        PieceOwnL2 = Teammates[pieceIndL2];

                                        if (PieceOwnL2 != null)
                                        {
                                            if (PieceOwnL2.isAlive)
                                            {
                                                movesArrL2 = PieceOwnL2.DefineMoves(out NumL2, lastStep, true);

                                                if (!Defense(PieceOwnL2, movesArrL2, NumL2, lastStep))
                                                {
                                                    threatNum++;
                                                }

                                                totNumL2 += NumL2;
                                                totCapL2 += PieceOwnL2.MaxMoves;
                                            }
                                            else if (DefenseStatus[pieceIndL2])
                                            {
                                                totCapL2 += PieceOwnL2.MaxMoves;
                                            }        
                                        }   
                                    } // end for (COLLECTING L2 DATA)

                                    Board.AIundoMove(nextStepData, nextStep); // --- UNDO RIVAL MOVE -------------------
                                    // ------- LEVEL 1 -----------------------------------------------------------------

                                    // --- DEFINE MINMAX -------------------------
                                    if (threatNum > maxThreat) maxThreat = threatNum;
                                    else if (threatNum < minThreat) minThreat = threatNum;

                                    if (totNumL2 < minNum) minNum = totNumL2;
                                    else if (totNumL2 > maxNum) maxNum = totNumL2;

                                    if (totCapL2 < minCap) minCap = totCapL2;
                                    else if (totCapL2 > maxCap) maxCap = totCapL2;

                                } // end for (TRAVERSE OF RIVAL MOVES)
                            }     
                        } // end for (TRAVERSE OF RIVAL PIECES)

                        if (maxThreat < minThreat) maxThreat = minThreat;
                        if (maxNum < minNum) maxNum = minNum;
                        if (maxCap < minCap) maxCap = minCap;

                        LossRival = IniCapRival - totCapRival;
                        currDiff = LossRival - (currCapacity - minCap);

                        if (currDiff > LossDiff[index]) { /*FOR DEBUG*/ };

                        // --- COMPARISION OF THE MOVE DATA ------------
                        replaceFlag = false;
                        DataPly = MovesData[index];
                        // DataPly = {0 pieceInd, 1 moveInd, 2 NumRival, 3 CapRival, 4 minNum, 5 minCap, 6 maxNum, 7 maxCap, 8 maxThreat, 9 minThreat, 10 attackNum } 

                        currCrit = "";

                        if (minNum != 0 || DataPly[4] == 0)
                        {
                            if (totNumRival == 0)
                            {
                                if (DataPly[2] == 0)
                                {
                                    if (index < MovesData.GetLength(0)) // write equivalent move
                                    {
                                        MovesData[++index] = new byte[11] { pieceInd, moveInd, totNumRival, totCapRival, minNum, minCap, maxNum, maxCap, maxThreat, minThreat, attackNum };
                                        criterionStr = "Vittoria !";
                                    }
                                    else
                                    {
                                        OutCode = -1;
                                        break; // >>>>>>> OVERFLOW >>>>>>>
                                    }
                                }
                                else
                                {
                                    replaceFlag = true;
                                    currCrit = "Vittoria !";
                                }
                            }
                            else if (DataPly[2] != 0) // ------- // Analysis of the moves data in priority order // --------------- 
                            {
                                if ((minCap > DataPly[5] || minCap == currCapacity) && ( currDiff > LossDiff[index] ))
                                {
                                    replaceFlag = true;
                                    currCrit = "minCap AND Loss";
                                }
                                else if (minCap >= DataPly[5] || currDiff >= LossDiff[index] || LossRival == 27)
                                {
                                    bool Priority00, Priority00EQ, Priority01, Priority01EQ, Priority02, Priority02EQ;
                                    string Crit00, Crit01, Crit02;

                                    if (exchangeLogic)
                                    {
                                        Priority00 = (currDiff > LossDiff[index] || LossRival > priorLoss);
                                        Priority00EQ = (currDiff >= LossDiff[index] && LossRival >= priorLoss);
                                        Crit00 = "CapRival";

                                        Priority01 = (minCap > DataPly[5]);
                                        Priority01EQ = (minCap == DataPly[5]);
                                        Crit01 = "minCap";

                                        Priority02 = (maxThreat < DataPly[8]);
                                        Priority02EQ = (maxThreat == DataPly[8]);
                                        Crit02 = "maxThreat";
                                    }
                                    else // fortification logic
                                    {
                                        Priority00 = (minCap > DataPly[5] || currDiff > LossDiff[index]);
                                        Priority00EQ = (minCap == DataPly[5] && currDiff >= LossDiff[index]);
                                        Crit00 = "minCap";

                                        Priority01 = (maxThreat < DataPly[8]);
                                        Priority01EQ = (maxThreat == DataPly[8]);
                                        Crit01 = "maxThreat";

                                        Priority02 = (totCapRival < DataPly[3]);
                                        Priority02EQ = (totCapRival == DataPly[3]);
                                        Crit02 = "CapRival";
                                    }

                                    // --- Comparision analysis --------------------------------------------------------
                                    if (Priority00)
                                    {
                                        replaceFlag = true;
                                        currCrit = Crit00;
                                    }
                                    else if (Priority00EQ)
                                    {
                                        if (Priority01)
                                        {
                                            replaceFlag = true;
                                            currCrit = Crit01;
                                        }
                                        else if (Priority01EQ)
                                        {
                                            if (Priority02)
                                            {
                                                replaceFlag = true;
                                                currCrit = Crit02;
                                            }
                                            else if (Priority02EQ)
                                            {
                                                if (attackNum > DataPly[10])
                                                {
                                                    replaceFlag = true;
                                                    currCrit = "attackNum";
                                                }
                                                else if (attackNum == DataPly[10])
                                                {
                                                    if (minNum > DataPly[4])
                                                    {
                                                        replaceFlag = true;
                                                        currCrit = "minNum";
                                                    }
                                                    else if (minNum == DataPly[4])
                                                    {
                                                        if (totNumRival < DataPly[2])
                                                        {
                                                            replaceFlag = true;
                                                            currCrit = "NumRival";
                                                        }
                                                        else if (totNumRival == DataPly[2])
                                                        {
                                                            if (maxCap > DataPly[7])
                                                            {
                                                                replaceFlag = true;
                                                                currCrit = "maxCap";
                                                            }
                                                            else if (maxCap == DataPly[7])
                                                            {
                                                                if (maxNum > DataPly[6] && minThreat < DataPly[9])
                                                                {
                                                                    replaceFlag = true;
                                                                    currCrit = "maxNum AND minThreat";
                                                                }
                                                                else if (maxNum >= DataPly[6] || minThreat <= DataPly[9])
                                                                {
                                                                    if (index < MovesData.GetLength(0)) // write equivalent move
                                                                    {
                                                                        MovesData[++index] = new byte[11] { pieceInd, moveInd, totNumRival, totCapRival, minNum, minCap, maxNum, maxCap, maxThreat, minThreat, attackNum };
                                                                        LossDiff[index] = currDiff;
                                                                        criterionStr = "_";
                                                                    }
                                                                    else
                                                                    {
                                                                        OutCode = -1;
                                                                        break; // >>>>>>> OVERFLOW >>>>>>>
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                } 
                                            }
                                        }
                                    } // ------------------end of Comparision analysis --------------------------------------------------------
                                }
                            }
                        }

                        if (replaceFlag)
                        {
                            MovesData[0] = new byte[11] { pieceInd, moveInd, totNumRival, totCapRival, minNum, minCap, maxNum, maxCap, maxThreat, minThreat, attackNum };
                            LossDiff[0] = currDiff;
                            criterionStr = currCrit;
                            index = 0;

                            firstEntry = false;
                        }
                        else if (firstEntry)
                        {
                            MovesData[0] = new byte[11] { pieceInd, moveInd, totNumRival, totCapRival, minNum, minCap, maxNum, maxCap, maxThreat, minThreat, attackNum };
                            LossDiff[0] = currDiff;
                            criterionStr = "_first_entry";
                            index = 0;

                            firstEntry = false;
                        }
                        
                        Board.AIundoMove(currStepData, currStep); // --- UNDO OWN MOVE ---
                        // ------- LEVEL 0 -----------------------------------------------

                    } // end for (TRAVERSE OF OWN MOVES)
                }

                if (OutCode < 0)
                {
                    forecastStr = " Sally >: OVERFLOW!";

                    break; // >>>>>>> OVERFLOW >>>>>>>
                } 
            } // end for (TRAVERSE OF OWN PIECES)

            if (totNumTeam != 0)
            {
                int equalNum = index;

                Random RndIndex = new Random();
                index = RndIndex.Next(++index);

                DataPly = MovesData[index];

                totNumRival = DataPly[2];

                sbyte ifile = Arsenal[DataPly[0]][DataPly[1], 0];
                sbyte jrank = Arsenal[DataPly[0]][DataPly[1], 1];

                string pos = Teammates[DataPly[0]].posStr;

                Board.AIrecMove(currStep, Teammates[DataPly[0]], ifile, jrank, RivalZero: (totNumRival == 0)); // SALLY MAKES MOVE //

                // --- REPORT STRING ---------------------------------------------------------------------------
                if (forecastStr == "")
                {
                    forecastStr = String.Format(" Sally >: Forecast: minNum: {0,3} ; minCap: {1,3} ; RivalNum: {2,3} ; RivalCap: {3,3} ; maxThreats: {4,3}\n\r",
                        DataPly[4], DataPly[5], DataPly[2], DataPly[3], DataPly[8]);
                }

                StepString = String.Format("{0}{1}{2}", pos, chessman.strFiles[ifile], (++jrank).ToString()); // "e2e4"

                string strLogic;
                if (exchangeLogic)
                {
                    strLogic = "exchange Logic";
                }
                else
                {
                    strLogic = "fort Logic";
                }

                StepString = String.Format(" Sally >: {0} ; {1} ; Criterion: {2} ; equivalent moves: {3,3}", StepString, strLogic, criterionStr, equalNum);
                StepString = forecastStr + StepString;
                
                if (totNumRival == 0) // CHECKMATE : VICTORY ; OR  PATT
                {
                    OutCode = -127;
                    StepString += "\n\n\r Sally is very very happy ! :)))";
                }
                else
                {
                    OutCode = equalNum;
                }
            }
            else // (totNumTeam == 0) CHECKMATE : DEFEAT ; OR  PATT
            {
                OutCode = -128;
                Board.Scoresheet.Write(TeamZero: true);
                StepString += "\n\n\r Sally is dead (((...";
            }
            

            return OutCode;
        }

        public override void RestoreData() // intended for rewinding
        {
            OwnCapacity = 0;

            for (int i = 0; i < Teammates.GetLength(0); i++)
            {
                if (Teammates[i] != null && Teammates[i].isAlive) OwnCapacity += Teammates[i].MaxMoves;
            }

            StepString = "";
        }

        public Sally(Chessboard board, Colors color)
        {
            Board = board;

            if (color == Colors.Black)
            {
                Coalition = Colors.Black;
                Teammates = Board.BlackPieces;

                RivalColor = Colors.White;
                Rivals = Board.WhitePieces;
            }
            else
            {
                Coalition = Colors.White;
                Teammates = Board.WhitePieces;

                RivalColor = Colors.Black;
                Rivals = Board.BlackPieces;
            }

            OwnCapacity = 0;

            for (int i = 0; i < Teammates.GetLength(0); i++)
            {
                if (Teammates[i] != null) OwnCapacity += Teammates[i].MaxMoves;
            }
        }
    } // end of public abstract class ChessAI : ChessAI //////////////////////////////////////////////////////////////////

    public class TestAI : ChessAI
    {
        private byte pieceInd = 0;
        private byte moveInd = 0;
        private sbyte[,] movesArr;
        private sbyte[] StepInfo;
        private byte movesNum = 0;

        private bool toggle = true;
        private bool SW1 = true;
        
        public override chessman Testee
        {
            get { return Teammates[pieceInd]; }
        }

        public override int MakeMove()
        {
            int outCode = 0;

            if (toggle) // ----- DO ----------------------------------------------------
            {
                int currStep = Board.Scoresheet.StepNumber;

                StepInfo = new sbyte[7];

                if (SW1 || moveInd == 0)
                {
                    SW1 = false;
                    
                    movesNum = 0;
                    movesArr = Teammates[pieceInd].DefineMoves(out movesNum, currStep, true);
                }

                if (movesNum > 0)
                {
                    //Board.AItrialMove(StepInfo, currStep, Teammates[pieceInd], movesArr[moveInd, 0], movesArr[moveInd, 1]);
                    moveInd++;

                    toggle = false;

                    outCode = 1;
                }
                
                if (moveInd == movesNum)
                {
                    moveInd = 0;

                    if (++pieceInd > 23) pieceInd = 0;

                    while (Teammates[pieceInd] == null)
                    {
                        if (++pieceInd > 23) pieceInd = 0;
                    }

                    SW1 = true;
                }   
            }
            else // ----- UNDO ------------------------------------------------
            {
                Board.AIundoMove(StepInfo, Board.Scoresheet.StepNumber);

                toggle = true;

                outCode = -1;
            }
            
            return outCode;
        }

        public TestAI(Chessboard board, Colors color)
        {
            Board = board;

            if (color == Colors.Black)
            {
                Coalition = Colors.Black;
                ColorIndex = 0;
                Teammates = Board.BlackPieces;

                RivalColor = Colors.White;
                Rivals = Board.WhitePieces;
            }
            else
            {
                Coalition = Colors.White;
                ColorIndex = 1;
                Teammates = Board.WhitePieces;

                RivalColor = Colors.Black;
                Rivals = Board.BlackPieces;
            }
        }
    }
}