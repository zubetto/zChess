using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace zChess
{
    [Serializable]
    public struct GameData : ISerializable
    {
        private bool CustomIni;

        public Pieces[][] IniPieces; // { BlackPieces, WhitePieces }
        public bool[][] IniStates; // { BlackPieces.InInitial, WhitePieces.InInitial }
        public sbyte[][,] IniCoords; // { BlackPieces.posArr, WhitePieces.posArr }
        public sbyte[][] Scoresheet;

        public GameData(sbyte[][] scoresheet, int stepNum, chessman[][] iniMaterial = null)
        {
            if (iniMaterial == null)
            {
                CustomIni = false;

                IniPieces = null;
                IniStates = null;
                IniCoords = null;
            }
            else
            {
                CustomIni = true;

                int Num = iniMaterial[0].GetLength(0);

                IniPieces = new Pieces[2][] { new Pieces[Num], new Pieces[Num] };
                IniStates = new bool[2][] { new bool[Num], new bool[Num] };
                IniCoords = new sbyte[2][,] { new sbyte[Num, 2], new sbyte[Num, 2] };

                for (int i = 0; i < 0; i++)
                {
                    if (iniMaterial[0][i] != null) // BlackPieces data
                    {
                        IniPieces[0][i] = iniMaterial[0][i].piece;
                        IniStates[0][i] = iniMaterial[0][i].InInitial;

                        IniCoords[0][i, 0] = iniMaterial[0][i].posArr[0];
                        IniCoords[0][i, 1] = iniMaterial[0][i].posArr[1];
                    }

                    if (iniMaterial[1][i] != null) // WhitePieces data
                    {
                        IniPieces[1][i] = iniMaterial[1][i].piece;
                        IniStates[1][i] = iniMaterial[1][i].InInitial;

                        IniCoords[1][i, 0] = iniMaterial[1][i].posArr[0];
                        IniCoords[1][i, 1] = iniMaterial[1][i].posArr[1];
                    }
                }
            }
            
            Scoresheet = new sbyte[stepNum][];

            for (int i = 0; i < stepNum; i++)
            {
                Scoresheet[i] = scoresheet[i];
            }
        }

        // [][] [][] [][] [] ISerializable implement [] [][] [][] [][][][] [][][] [] [][][] [][]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("CustomIni", CustomIni);

            if (CustomIni)
            {
                info.AddValue("IniPieces", IniPieces, typeof(Pieces[][]));
                info.AddValue("IniStates", IniStates, typeof(bool[][]));
                info.AddValue("IniCoords", IniCoords, typeof(sbyte[][,]));
            }

            info.AddValue("Scoresheet", Scoresheet, typeof(sbyte[][]));
        }

        public GameData(SerializationInfo info, StreamingContext context)
        {
            CustomIni = info.GetBoolean("CustomIni");

            if (CustomIni)
            {
                IniPieces = (Pieces[][])info.GetValue("IniPieces", typeof(Pieces[][]));
                IniStates = (bool[][])info.GetValue("IniStates", typeof(bool[][]));
                IniCoords = (sbyte[][,])info.GetValue("IniCoords", typeof(sbyte[][,]));
            }
            else
            {
                IniPieces = null;
                IniStates = null;
                IniCoords = null;
            }

            Scoresheet = (sbyte[][])info.GetValue("Scoresheet", typeof(sbyte[][]));
        }
    }

    class Program
    {
        delegate void Outputs(string result);
        delegate string Inputs();
        delegate byte[] Interactor(string input, Inputs InDevice, Outputs OutDevice);
        delegate bool Comporator(byte[] Address1, byte[] Address2);

        static Interactor[] Navigators;
        static byte[][] NavDirectory = new byte[64][]; // contains addresses arrays, address[0] = 0 is reserved for Home, address[0] = 255 - for exit

        static GameData? LoadedGame = null;
        static string DefaultPath = ""; // for games saving
        static string LastSaveName = "";
        static string IOexceptionStr = "";
        static bool IOexceptionFlag = false; // if true user will not be able to save/open games
        static char[] invalidFileChars = Path.GetInvalidFileNameChars();
        static chessman[][] InitialMaterial = null;
        
        static ChessAI GameAI;
        static Chessboard Gamestuff;
        static string gameName = "";
        static Colors PlayerColor;
        static Colors AIcolor;
        static bool ColorSet = false;
        static bool PlayerStroke = false;
        static object Exchanger;
        static int ExchangerInt;

        public static string[] strFiles = new string[8] { "a", "b", "c", "d", "e", "f", "g", "h" };

        static bool A1_less_A2(byte[] A1, byte[] A2)
        {
            int Len1 = A1.GetLength(0);
            int Len2 = A2.GetLength(0);
            int Length;
            bool LenFlag;

            if (Len1 < Len2)
            {
                Length = Len1;
                LenFlag = true;
            } 
            else
            {
                Length = Len2;
                LenFlag = false;
            }

            for (int rank = 0; rank < Length; rank++)
            {
                if (A1[rank] < A2[rank])
                {
                    return true;
                }
                else if (A1[rank] > A2[rank])
                {
                    return false;
                }
            }

            return LenFlag;
        }

        static bool Sort(Comporator Compare = null)
        {
            Compare = Compare ?? A1_less_A2;

            int Num = NavDirectory.GetLength(0);

            if (Num != Navigators.GetLength(0)) return false; // >>>>> zChess: LENGTHS OF DIRECTORY AND NAVIGATORS ARE NOT EQUAL

            for (int i = 1; i < Num; i++)
            {
                if (NavDirectory[i] == null)
                {
                    Num = i;
                    break;
                }
            }
            
            int[] sortedIndexes = new int[Num];
            int currIndex = 0;
            
            for (int ind = 1; ind < Num; ind++)
            {
                sortedIndexes[ind] = ind;
                currIndex = ind;

                for (int j = 0; j < ind; j++)
                {
                    if ( Compare(NavDirectory[ind], NavDirectory[j]) ) // by default: if Directory[ind] < Directory[j]
                    {
                        sortedIndexes[j]++;
                        currIndex--;
                    }
                }

                sortedIndexes[ind] = currIndex;
            }

            byte[][] DirTmp = new byte[Num][];
            Interactor[] NavTmp = new Interactor[Num];

            for (int i = 0; i < Num; i++)
            {
                DirTmp[sortedIndexes[i]] = NavDirectory[i];
                NavTmp[sortedIndexes[i]] = Navigators[i];
            }

            NavDirectory = DirTmp;
            Navigators = NavTmp;

            return true;
        }

        static Interactor Search(byte[] address, out int index)
        {
            if (address[0] == 0)
            {
                index = 0;
                return Navigators[0]; // >>>>> returns Home >>>>>
            }

            int DirNum = NavDirectory.GetLength(0);
            int length = address.GetLength(0);
            int lenDir = 0;
            index = 1;

            bool found = false;

            for (int rank = 0; rank < length; rank++)
            {
                found = false;

                for (int i = index; i < DirNum; i++)
                {
                    lenDir = NavDirectory[i].GetLength(0);

                    if (rank < lenDir && address[rank] == NavDirectory[i][rank])
                    {
                        index = i;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    index = -1;
                    return null; // >>>>> non-existent address >>>>>
                }
            }

            return Navigators[index];
        }

        static string movesArrToString(sbyte[,] movesArr)
        {
            string outStr = "";
            string fileStr = "";
            string Lstr = "";
            int L = 8;

            sbyte coord = 0;
            int num = movesArr.GetLength(0);
            sbyte rplus = 0;
            int iplus = 0;

            for (int i = 0; i < num; i++)
            {
                coord = movesArr[i, 0];
                iplus = i + 1;
                Lstr = "";

                if (iplus >= L && iplus % L == 0) Lstr = "\n\r   ";

                if (coord >= 0)
                {
                    fileStr = strFiles[coord];
                    rplus = movesArr[i, 1];
                    outStr += String.Format("| {0,2}:  {1}{2} {3}", iplus, fileStr, ++rplus, Lstr);
                }
                else if (coord != -128) // mark X for "under shah" moves
                {
                    fileStr = strFiles[coord + 127];
                    rplus = movesArr[i, 1];
                    outStr += String.Format("| {0,2}:XX{1}{2} {3}", iplus, fileStr, ++rplus, Lstr);
                }
            }

            return outStr;
        }

        static sbyte[] StringToVector(string input, out bool complete)
        {
            complete = false;

            int inputLength = input.Length;
            if (inputLength != 2 && inputLength != 4) return null; // >>>>> INVALID INPUT >>>>>

            sbyte[] Vector = new sbyte[inputLength];
            sbyte iplus = 0;
            sbyte rank = 0;
            string inputSub = "";

            for (sbyte i = 0; i < inputLength; i += 2)
            {
                inputSub = input.Substring(i, 1);

                for (sbyte j = 0; j < 8; j++)
                {
                    if (inputSub == strFiles[j])
                    {
                        Vector[i] = j; // file set
                        complete = true;
                        break;
                    }
                }

                if (!complete) return null; // >>>>> INVALID INPUT >>>>>

                iplus = i; iplus++;
                inputSub = input.Substring(iplus, 1);

                complete = false;
                complete = sbyte.TryParse(inputSub, out rank);

                if (complete && rank >= 1 && rank <= 8)
                {
                    rank--;
                    Vector[iplus] = rank; // rank set
                }
                else
                {
                    complete = false;
                    return null; // >>>>> INVALID INPUT >>>>>
                }
            }

            return Vector;
        }

        static string MoveCodeToString(sbyte code)
        {
            string OutStr;

            switch (code)
            {
                case 5:
                    OutStr = "KINGSIDE CASTLING";
                    break;
                case 4:
                    OutStr = "QUEENSIDE CASTLING";
                    break;
                case 3:
                    OutStr = "PAWN PROMOTION ! YOU SHOULD SELECT ANY PIECE TO REPLACE";
                    break;
                case 2:
                case 1:
                    OutStr = "PLAYER HAS MADE THE MOVE";
                    break;
                case 0:
                    OutStr = "COORDS OF THE FIRST CELL IS INVALID";
                    break;
                case -1:
                    OutStr = "HANDS OFF MY CHESSMAN !";
                    break;
                case -2:
                    OutStr = "COORDS OF THE SECOND CELL IS INVALID";
                    break;
                case -3:
                    OutStr = "INVALID MOVE";
                    break;
                case -4:
                    OutStr = "THE KING IN CHECK";
                    break;
                case -5:
                    OutStr = "CASTLING IS PROHIBITED";
                    break;
                default:
                    OutStr = "?An unknown gizmo?";
                    break;
            }

            return OutStr;
        }

        static void Display(Outputs OutDev, bool board = false, int turns = 8)
        {
            int Line = Gamestuff.Scoresheet.TurnNumber;

            if (turns > Line)
            {
                turns = Line + 1;
                Line = 0;
            }
            else
            {
                Line -= turns - 1;
                turns++;
            }

            string currStr = "";
            string pasteStr = "";
            string colorStr = "";
            string pieceStr = "";
            //string pieceStr2 = "";

            chessman Owner = null;

            if (board) // display chessboard
            {
                currStr = "    ------------------------------- ";

                if (turns > 0)
                {
                    currStr += "      ";
                    currStr += Gamestuff.Scoresheet.firstLine;
                }

                OutDev(currStr);
                
                for (int j = 7; j > -1; j--)
                {
                    currStr = String.Format(" {0} |", j+1);

                    for (int i = 0; i < 8; i++)
                    {
                        Owner = Gamestuff.Require(i, j);

                        if (Owner == null || !Owner.isAlive)
                        {
                            pasteStr = " _ ";
                        }
                        else
                        {
                            colorStr = Owner.color.ToString();
                            colorStr = colorStr.Substring(0, 1);
                            colorStr = colorStr.ToLower();

                            pieceStr = Owner.Symbol;

                            pasteStr = colorStr + pieceStr;
                        }

                        currStr += String.Format("{0}|", pasteStr);
                    } // end for (int j = 0; j < 7; j++)

                    if (turns-- > 0)
                    {
                        pasteStr = Gamestuff.Scoresheet.infoLine(Line++);

                        if (pasteStr != "")
                        {
                            currStr += String.Format("  {0}", pasteStr);
                        }  
                    }

                    OutDev(currStr);
                } // end for (int i = 7; i > -1; i--)

                OutDev("    ------------------------------- ");
                OutDev("     a   b   c   d   e   f   g   h ");
            }

            OutDev(String.Format("\n\r stroke: {0,3}", Gamestuff.Scoresheet.TurnNumber + 1));

            if (GameAI != null) OutDev(String.Format("{0}\n\r", GameAI.StepString));
        }

        /// <summary>
        /// Loads game data from specified file ; out string is empty if success or has an error message if loading failed
        /// </summary>
        /// <param name="name">valid absolute filename</param>
        /// <returns>nullable GameData struct</returns>
        static GameData? LoadGame(string name, out string resultStr)
        {
            GameData? LoadedData = null;
            resultStr = "";

            if (name != "")
            {
                FileStream OpenStream = null;
                bool loaded = true;

                try
                {
                    BinaryFormatter binaryFmt = new BinaryFormatter();

                    OpenStream = new FileStream(name, FileMode.Open);

                    LoadedData = (GameData)binaryFmt.Deserialize(OpenStream);

                }
                catch (Exception e)
                {
                    loaded = false;
                    resultStr = "Unable to load the game:\n\r" + e.ToString();
                }
                finally
                {
                    if (OpenStream != null) OpenStream.Close();
                }

                if (!loaded)
                {
                    return null; // >>>>> EXCEPTION DURIN LOADING >>>>>
                }
            }
            else
            {
                resultStr = "Filename is not specified";
            }

            return LoadedData;
        }

        /// <summary>
        /// Saves game data under given name and with extension .zus
        /// </summary>
        /// <param name="name">valid absolute filename</param>
        /// <returns>empty string if success or error message if failed</returns>
        static string SaveGame(string name, Journal txtData)
        {
            string resultStr = "";

            if (name != "")
            {
                FileStream SaveStream = null;

                bool binOk = true;

                // Saving of the GameData
                try
                {
                    GameData SaveData = new GameData(Gamestuff.Scoresheet.Protocol, Gamestuff.Scoresheet.StepTotal, InitialMaterial);
                    BinaryFormatter binaryFmt = new BinaryFormatter();

                    SaveStream = new FileStream(name, FileMode.Create);

                    binaryFmt.Serialize(SaveStream, SaveData);
                }
                catch (Exception e)
                {
                    binOk = false;
                    resultStr = "Unable to save the game:\n\r" + e.ToString();
                }
                finally
                {
                    if (SaveStream != null) SaveStream.Close();
                }

                // Saving Scoresheet into readable .txt file
                if (binOk)
                {
                    string txtName = Path.ChangeExtension(name, ".txt");
                    StreamWriter txtStream = null;

                    try
                    {
                        txtStream = File.CreateText(txtName);

                        txtStream.WriteLine(">>>>>>> zChess \"{0}\" <<<<<<<", Path.GetFileName(name));
                        txtStream.WriteLine(" ");
                        txtStream.WriteLine("       {0}", txtData.firstLine);
                        txtStream.WriteLine("    ----------------------------------------------");

                        for (int i = 0; i <= txtData.TurnNumber; i++)
                        {
                            txtStream.WriteLine(txtData.infoLine(i));
                        }
                    }
                    catch (Exception e)
                    {
                        resultStr += String.Format("\n\r -------------------------\n\n\r Unable to save scoresheet:\n\r", e.ToString());
                    }
                    finally
                    {
                        if (txtStream != null) txtStream.Close();
                    }
                }  
            }
            else
            {
                resultStr = "Filename is not specified";
            }

            return resultStr;
        }

        //------- [ Menus handlers ] ------------------------------------------------------------
        /// <summary>
        /// L0_Home address: { 0 }
        /// </summary>
        static byte[] L0_Home(string input, Inputs _d, Outputs device)
        {
            // Reseting game variables ------------------
            Gamestuff = null;
            gameName = "";
            ColorSet = false;

            Exchanger = null;
            ExchangerInt = 0;

            LoadedGame = null;
            LastSaveName = "";
            // end of Reseting game variables -----------

            byte[] address = new byte[1] { 0 };
            string[] L0menu = new string[4] { "help", "new", "open", "exit" };
            byte MenuItemsNum = (byte)L0menu.GetLength(0);

            int inputInd;
            bool confirm = Int32.TryParse(input, out inputInd);
            byte ind = 0;

            if (confirm)
            {
                if (inputInd > 0 && inputInd < MenuItemsNum) ind = (byte)inputInd;
            }
            else
            {
                for (byte i = 0; i < MenuItemsNum; i++)
                {
                    if (input == L0menu[i])
                    {
                        ind = i;
                        break;
                    }
                }
            }

            if (ind > 0)
            {
                address[0] += ind;
            }
            else // stay here and show the menu
            {
                address = null;
                
                device(">>>>>>> zChess [Home] <<<<<<<\r\n");

                string MenuItems = "";
                
                for (int i = 1; i < MenuItemsNum; i++)
                {
                    MenuItems = String.Format("{0} - {1}", i, L0menu[i]);
                    device(MenuItems);
                }

                device("-------");
            }

            return address;
        }

        /// <summary>
        /// L1_Open address: { 2 }
        /// </summary>
        static byte[] L1_Open(string input, Inputs InDev, Outputs OutDev)
        {
            byte[] address = null;
            string resultStr = " Enter file name or:";

            OutDev(">>>>>>> zChess [Home/Open] <<<<<<<\r\n");

            if (IOexceptionFlag)
            {
                OutDev(String.Format(" >: Unable to open:\n\r{0}\n\r >>> Press Enter to go home menu <<<", IOexceptionStr));
                InDev();

                return new byte[1] { 0 }; // >>>>> UNABLE TO OPEN DUE TO IOexceptionFlag IS TRUE ; { 0 } --> L0_Home >>>>>
            }

            input = input.Trim();
            bool scrollUp = false;

            if (!String.IsNullOrWhiteSpace(input))
            {
                int Ind = input.IndexOfAny(invalidFileChars);

                if (Ind < 0) // thera are no invalid char in input
                {
                    int inpLen = input.Length;
                    string relFilename = input;
                    string absFilename = input;

                    if (inpLen <= 4 || (inpLen > 4 && input.Substring(inpLen - 4) != ".zus")) // checking of extension presence in the input
                    {
                        absFilename += ".zus";
                    }
                    else
                    {
                        relFilename = input.Substring(0, inpLen - 4);
                    }

                    absFilename = Path.Combine(DefaultPath, absFilename);

                    if (File.Exists(absFilename))
                    {
                        string errStr = "";

                        LoadedGame = LoadGame(absFilename, out errStr);

                        if (errStr == "")
                        {
                            gameName = relFilename;

                            if (LoadedGame.Value.IniPieces == null)
                            {
                                Gamestuff = new Chessboard(name: gameName);
                                Gamestuff.Scoresheet.isReady = false; // scoresheet restoring will be required after player will have selected color
                            }
                            else
                            {
                                // recovering initial position
                            }

                            ExchangerInt = 0;
                            return new byte[2] { 1, 0 }; // >>>>> --> L2_Begin >>>>>
                        }
                        else
                        {
                            resultStr = String.Format(" >: Unable to open {0}.zus:\n\r{1}", relFilename, errStr);
                        }
                    }
                    else
                    {
                        resultStr = String.Format(" >: File {0}.zus does not exists", relFilename);
                    }
                }
                else if (input == ">>") // scroll forward
                {
                    scrollUp = true;
                }
                else if (input == "<<") // scroll backward
                {
                    ExchangerInt -= 20;
                    if (ExchangerInt < 0) ExchangerInt = 0;
                }
                else if (input == "><")
                {
                    ExchangerInt = 0;
                    return new byte[1] { 0 }; // >>>>> --> L0_Home >>>>>
                }
                else
                {
                    resultStr = String.Format(" \"{0}\" is invalid character in file name", input.Substring(Ind, 1));
                }
            } // end if (!String.IsNullOrWhiteSpace(input))
            
            OutDev(String.Format("Search in >: {0}\n\r______________________\n\r", DefaultPath));

            try
            {
                string[] zusFiles = Directory.GetFiles(DefaultPath, "*.zus");

                int filesNum = zusFiles.GetLength(0);

                if (scrollUp)
                {
                    int tmpInd = ExchangerInt + 20;

                    if (tmpInd < filesNum)
                    {
                        ExchangerInt = tmpInd;

                    }
                }

                int remainNum = filesNum - ExchangerInt;

                if (remainNum > 10) // output file names in two columns
                {
                    int pairsNum = 0;
                    int lastNum = 0;

                    if (remainNum < 20)
                    {
                        pairsNum = ExchangerInt + remainNum - 10;
                        lastNum = ExchangerInt + 10;
                    }
                    else
                    {
                        pairsNum = ExchangerInt + 10;
                        lastNum = 0;
                    }

                    for (int i = ExchangerInt; i < pairsNum; i++)
                    {
                        OutDev(String.Format(" {0,-40} {1,-40}", Path.GetFileName(zusFiles[i]), Path.GetFileName(zusFiles[i + 10]) ));
                    }

                    for (int i = pairsNum; i < lastNum; i++)
                    {
                        OutDev(String.Format(" {0}", Path.GetFileName(zusFiles[i]) ));
                    }
                }
                else // output file names in one column
                {
                    for (int i = ExchangerInt; i < filesNum; i++)
                    {
                        OutDev(String.Format(" {0}", Path.GetFileName(zusFiles[i])));
                    }
                }
            }
            catch (Exception e)
            {
                OutDev(e.ToString());
            }

            OutDev("\n\r______________________\n\r");

            if (resultStr != "") OutDev(String.Format("{0}\n\r", resultStr));
            else OutDev("\n\r");

            OutDev(">> - scroll forward");
            OutDev(">> - scroll backward");
            OutDev(">< - return to home menu\n\n\r-------");

            return address;
        }

        /// <summary>
        /// L1_New address: { 1 }
        /// </summary>
        static byte[] L1_New(string input, Inputs InDev, Outputs OutDev)
        {
            if (gameName == "")
            {
                OutDev(">>>>>>> zChess [Home/New] <<<<<<<\r\n");
                OutDev("game name >: ");
                gameName = InDev();

                if (String.IsNullOrWhiteSpace(gameName))
                {
                    gameName = "New Game";
                }
                else
                {
                    gameName = gameName.Trim();
                } 

                return new byte[1] { 1 };
            }

            byte[] address = new byte[2] { 1, 0 };

            string[] menuL1New = new string[3] { "begin", "set", "cancel" };
            string[] menuDesc = new string[3] { "to play", "arrangement of pieces", "and go home" };
            byte MenuItemsNum = (byte)menuL1New.GetLength(0);

            int inputInd;
            bool confirm = Int32.TryParse(input, out inputInd);
            byte ind = 3;

            if (confirm)
            {
                if (inputInd > -1 && inputInd < MenuItemsNum) ind = (byte)inputInd;
            }
            else
            {
                for (byte i = 0; i < MenuItemsNum; i++)
                {
                    if (input == menuL1New[i])
                    {
                        ind = i;
                        break;
                    }
                }
            }

            if (ind == 2) // cancel
            {
                gameName = "";
                return new byte[1] { 0 };
            }
            else if (ind != 3)
            {
                address[1] += ind;
            }
            else
            {
                address = null;

                OutDev(String.Format(">>>>>>> zChess \"{0}\" [Home/New] <<<<<<<\r\n", gameName));

                string MenuItems = "";

                for (int i = 0; i < MenuItemsNum; i++)
                {
                    MenuItems = String.Format("{0} - {1} {2}", i, menuL1New[i], menuDesc[i]);
                    OutDev(MenuItems);
                }

                OutDev("-------");
            }

            return address;
        }

        // L2_Begin MENU ITEMS ------------------------------------------------------------------------------------------------------------
        static string[] menuNcommSTR = new string[12]
            { "save as", "save", "undo", "redo", "rewind", "options", "exit", "help", "e2e4", "0-0", "0-0-0", "e4" };

        static string[] menuNcommKEY = new string[12]
        { "      ", "CTRL+S", "CTRL+Z", "CTRL+Y", "      ", "CTRL+O", "      ", "CTRL+F1", "e2e4", "0-0", "0-0-0", "e4" };

        static string[] menuNcommDES = new string[12]
        { "save scoresheet", "quick save", "one stroke backward", "one stroke forward", "go to specified stroke (ex.: \"rewind 21\")",
            "scoresheet and info display options", "exit to home menu", "CTRL+F1",
               "make move", "kingside castling", "queenside castling", "show cell info" };

        /// <summary>
        /// GAMEPLAY ; 
        /// L2_Begin address: { 1, 0 }
        /// </summary>
        static byte[] L2_Begin(string input, Inputs InDev, Outputs OutDev)
        {
            if (Gamestuff == null || !ColorSet) // the last settings ////////////////////////////////////////
            {
                if (input == "b" || input == "B" || input == "black" || input == "Black")
                {
                    PlayerColor = Colors.Black;
                    AIcolor = Colors.White;
                    PlayerStroke = false;
                }
                else if (input == "w" || input == "W" || input == "white" || input == "White")
                {
                    PlayerColor = Colors.White;
                    AIcolor = Colors.Black;
                    PlayerStroke = true;
                }
                else if (input == "s" || input == "S" || input == "study" || input == "Study")
                {
                    PlayerColor = Colors.nil;
                    AIcolor = Colors.nil;
                    PlayerStroke = true;
                }
                else
                {
                    OutDev(String.Format(">>>>>>> zChess \"{0}\" [Home/New/Begin] <<<<<<<\r\n", gameName));
                    OutDev("black or white? or study? >:");
                    return null;
                }

                ColorSet = true;
                Gamestuff = Gamestuff ?? new Chessboard(name: gameName);

                if (!Gamestuff.Scoresheet.isReady)
                {
                    if (PlayerColor != Colors.nil) Gamestuff.Scoresheet.Load(LoadedGame.Value.Scoresheet, color: PlayerColor);
                    else Gamestuff.Scoresheet.Load(LoadedGame.Value.Scoresheet);

                    PlayerStroke = true;
                }

                if (AIcolor != Colors.nil)
                {
                    GameAI = new Sally(Gamestuff, AIcolor);
                    //GameAI = new TestAI(Gamestuff, AIcolor);

                    Gamestuff.Scoresheet.AIdata = GameAI;
                }
                
                return new byte[2] { 1, 0 };
            } // end if (Gamestuff == null || !ColorSet) //////////////////////////////////////////////////////

            // layout:
            // [ input ]
            // [ if PlayerStroke and valid input then Player move, PlayerStroke = false ]
            // [ if !PlayerStroke then AI move, PlayerStroke = true ] 
            // [ show menu items ] 
            // --> go to input (with external while loop)

            int itemNum = menuNcommSTR.GetLength(0);
            byte[] address = null;
            bool checkFurther = true;
            string InputsComment = "";

            /*// -------- FOR DEBUGGING --------------------------------------------
            if (input == ">:0x0000")
            {
                PlayerStroke = !PlayerStroke;
            } // ----------------------------------------------------------------- */

            // GAMEPLAY ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            if (PlayerStroke && (!Gamestuff.Scoresheet.isOver || Gamestuff.Scoresheet.StepNumber <= Gamestuff.Scoresheet.LastStep))
            {
                if (input == "0-0" && PlayerColor != Colors.nil) // kingside castl
                {
                    sbyte OutCode = Gamestuff.Castling(PlayerColor, true);

                    InputsComment = String.Format(">: {0}\n\r", MoveCodeToString(OutCode));

                    if (OutCode > 0)
                    {
                        PlayerStroke = false;
                        address = new byte[2] { 1, 0 };
                    }
                }
                else if (input == "0-0-0" && PlayerColor != Colors.nil) // queenside castl
                {
                    sbyte OutCode = Gamestuff.Castling(PlayerColor, false);

                    InputsComment = String.Format(">: {0}\n\r", MoveCodeToString(OutCode));

                    if (OutCode > 0)
                    {
                        PlayerStroke = false;
                        address = new byte[2] { 1, 0 };
                    }
                }
                else // --- checking if the input is a vector of move ------------------------------------------
                {
                    bool validInput = false;
                    sbyte[] moveVector = StringToVector(input, out validInput);

                    if (validInput)
                    {
                        checkFurther = false;

                        if (input.Length == 4)
                        {
                            sbyte[,] ValidMoves = null;
                            sbyte OutCode;

                            if (PlayerColor != Colors.nil)
                            {
                                OutCode = Gamestuff.PlayerMove(moveVector, PlayerColor, ref ValidMoves);
                            }
                            else
                            {
                                OutCode = Gamestuff.PlayerMove(moveVector, Colors.Black, ref ValidMoves);

                                if (OutCode == -1)
                                {
                                    OutCode = Gamestuff.PlayerMove(moveVector, Colors.White, ref ValidMoves);
                                }
                            }

                            InputsComment = String.Format(">: {0}\n\r", MoveCodeToString(OutCode));

                            if (OutCode == 3) // ----------- promotion -------------------------------------------
                            {
                                Exchanger = moveVector;
                                return new byte[3] { 1, 0, 2 }; // >>>>> goto PROMOTION menu >>>>>
                            }
                            else if (OutCode > 0 && PlayerColor != Colors.nil) // --- player has made a move -----
                            {
                                PlayerStroke = false;
                                address = new byte[2] { 1, 0 };
                            }
                            else if (OutCode <= -3 && ValidMoves != null) // --- then show ValidMoves -------------
                            {
                                string movesStr = movesArrToString(ValidMoves);
                                InputsComment = String.Format("{0}>: {1}\n\r", InputsComment, movesStr);

                                // !!!!!!! FOR DEBUG ONLY !!!!!!!
                                sbyte[,] VerifiedMoves = null;
                                byte Num;
                                chessman Owner = Gamestuff.Require(moveVector[0], moveVector[1]);
                                VerifiedMoves = Owner.DefineMoves(out Num, refined: true);
                                movesStr = movesArrToString(VerifiedMoves);

                                InputsComment = String.Format("{0}\n\r>: VerifiedMoves:\n\r>: {1}\n\r", InputsComment, movesStr);
                            }
                        }
                        else // input.Length == 2, show cell info
                        {
                            chessman Owner = Gamestuff.Require(moveVector[0], moveVector[1]);

                            if (Owner == null)
                            {
                                InputsComment = ">: THE CELL IS EMPTY\n\r";
                            }
                            else
                            {
                                InputsComment = String.Format(">: {0} {1}\n\r", Owner.color, Owner.piece);
                            }
                        }
                    }
                    else
                    {
                        checkFurther = true;
                    }    
                }
            } // end if (PlayerStroke)
            else if (!PlayerStroke && AIcolor != Colors.nil) // STROKE OF THE AI ////////////////////////////////////////////////////////////////
            {
                int code = GameAI.MakeMove();

                //if (code < 0) AIcolor = Colors.nil; // GAME OVER

                PlayerStroke = true;
                checkFurther = false;
            } // END OF GAMEPLAY ////////////////////////////////////////////////////////////////////////////////////////////////////////////////


            if (checkFurther) // ---------------------------------------------------------------------------------------------------
            {
                string Digit = "";
                if (input != "") Digit = input.Substring(0, 1);

                string rewStr = "";
                if (input.Length > 6) rewStr = input.Substring(0, 6);


                // checking of the input ----------------------------------------------------------------
                if (input == "0" || input == menuNcommSTR[0] || input == menuNcommKEY[0]) // "save as"
                {//--------------------------------------------------------------------------------------
                    address = new byte[3] { 1, 0, 0 };
                }
                else if (input == "1" || input == menuNcommSTR[1] || input == menuNcommKEY[1]) // "save"
                {//--------------------------------------------------------------------------------------
                    if (LastSaveName != "")
                    {
                        string errStr = SaveGame(LastSaveName, Gamestuff.Scoresheet);

                        if (errStr == "")
                        {
                            InputsComment = "Game was successfully saved";
                        }
                        else
                        {
                            InputsComment = "Unable to save the game";
                        }
                    }
                    else
                    {
                        address = new byte[3] { 1, 0, 0 };
                    }
                }
                else if (input == "2" || input == menuNcommSTR[2] || input == menuNcommKEY[2]) // "undo"
                {//--------------------------------------------------------------------------------------
                    Colors color = PlayerColor;
                    if (color == Colors.nil) color = Colors.White;

                    string stepStr = Gamestuff.Scoresheet.Rewind(Gamestuff.Scoresheet.TurnNumber - 1, color);

                    InputsComment = ">: Override move: " + stepStr;
                }
                else if (input == "3" || input == menuNcommSTR[3] || input == menuNcommKEY[3]) // "redo"
                {//--------------------------------------------------------------------------------------
                    Colors color = PlayerColor;
                    if (color == Colors.nil) color = Colors.White;

                    string stepStr = Gamestuff.Scoresheet.Rewind(Gamestuff.Scoresheet.TurnNumber + 1, color);

                    InputsComment = ">: Override move: " + stepStr;
                }
                else if ((Digit == "4" && input.Length > 2) || (rewStr == menuNcommSTR[4] && input.Length > 6)) // "rewind"
                {//---------------------------------------------------------------------------------------------------------
                    int stroke = 0;
                    string inpSub = "";

                    if (Digit == "4") inpSub = input.Substring(2);
                    else inpSub = input.Substring(6);

                    if (Int32.TryParse(inpSub, out stroke))
                    {
                        Colors color = PlayerColor;
                        if (color == Colors.nil) color = Colors.White;

                        string stepStr = Gamestuff.Scoresheet.Rewind(stroke - 1, color);

                        InputsComment = ">: Override move: " + stepStr;
                    }
                }
                else if (input == "5" || input == menuNcommSTR[5] || input == menuNcommKEY[5]) // "options"
                {//-----------------------------------------------------------------------------------------
                    address = new byte[3] { 1, 0, 1 };
                }
                else if (input == "6" || input == menuNcommSTR[6] || input == menuNcommKEY[6]) // "exit"
                {//--------------------------------------------------------------------------------------
                    address = new byte[3] { 1, 0, 4 };
                }
                else if (input == "7" || input == menuNcommSTR[7] || input == menuNcommKEY[7]) // "help"
                {//--------------------------------------------------------------------------------------
                    OutDev(String.Format(">>>>>>> zChess \"{0}\" [Help] <<<<<<<", gameName));

                    for (int i = 0; i < 7; i++)
                    {
                        OutDev(String.Format("{0} - {1} \t- {2} \t- {3}", i, menuNcommSTR[i], menuNcommKEY[i], menuNcommDES[i]));
                    }

                    OutDev(" ");

                    for (int i = 8; i < 12; i++)
                    {
                        OutDev(String.Format("{0} \t- {1}", menuNcommSTR[i], menuNcommDES[i]));
                    }

                    OutDev("-------");

                    return null; // >>>>> Show help items >>>>>
                }    
            } // end if (checkFurther)

            // OUTPUT OF THE MAIN INFO ///////////////////////////////////////////////////////////////////////////////////
            OutDev(String.Format(">>>>>>> zChess \"{0}\" <<<<<<<", gameName));
            Display(OutDev, board: true); // shows chessboard, scoresheet entries up to current ply and and AI info

            if (InputsComment == "") InputsComment = "| help - CTRL+F1 |\n\r";

            OutDev(String.Format("{0}\n\r-------", InputsComment));

            return address;
        }

        /// <summary>
        /// Save As ; 
        /// L3_SaveAs address: { 1, 0, 0 }
        /// </summary>
        static byte[] L3_SaveAs(string input, Inputs InDev, Outputs OutDev)
        {
            byte[] address = null;

            OutDev(String.Format(">>>>>>> zChess \"{0}\" [Save As] <<<<<<<\n\r", gameName));

            if (IOexceptionFlag)
            {
                OutDev(String.Format(" >: Unable to save:\n\r{0}\n\r >>> Press Enter to resume the game <<<", IOexceptionStr));
                InDev();

                return new byte[2] { 1, 0 }; // >>>>> UNABLE TO SAVE DUE TO IOexceptionFlag IS TRUE ; { 1, 0 } --> L2_Begin >>>>>
            }
            else if (Gamestuff.Scoresheet.StepNumber == 0)
            {
                OutDev(" >: Scoresheet is empty\n\r >>> Press Enter to resume the game <<<");
                InDev();

                return new byte[2] { 1, 0 }; // >>>>> UNABLE TO SAVE DUE TO EMPTY SCORESHEET ; { 1, 0 } --> L2_Begin >>>>>
            }

            OutDev(String.Format("Save path >: {0}", DefaultPath));

            bool inputNull = String.IsNullOrWhiteSpace(input);
            string lastInput = Exchanger as string;

            if (inputNull && lastInput == null)
            {
                Exchanger = gameName;

                OutDev(String.Format("\n\rFile name >: {0}", gameName));
                OutDev("-------");
            }
            else
            {
                if (inputNull) input = lastInput;

                input = input.Trim();

                Exchanger = input;

                int Ind = input.IndexOfAny(invalidFileChars);

                if (Ind < 0) // there are no invalid char in the input
                {
                    address = new byte[4] { 1, 0, 0, 0 }; // go to SaveAs Confirmation menu
                }
                else // invalid char was found in the input
                {
                    OutDev(String.Format("\n\rFile name >: {0}", input));
                    OutDev(String.Format("\"{0}\" is invalid character in file name", input.Substring(Ind, 1)));
                    OutDev("-------");
                }
            }
            
            return address;
        }

        /// <summary>
        /// Save As Confirmation; 
        /// L4_SaveAs address: { 1, 0, 0, 0 }
        /// </summary>
        static byte[] L4_SaveAsCnfrm(string input, Inputs InDev, Outputs OutDev)
        {
            byte[] address = null;

            string inputName = Exchanger as string;

            if (inputName == null)
            {
                OutDev(">>>>>>> zChess [Save As/Confirmation/ERROR] <<<<<<<\n\r");
                OutDev(" zChess >: ERROR : Exchanger is not valid\n\n\r >>> Press Enter to resume the game <<<");
                InDev();

                return new byte[2] { 1, 0 }; // >>>>> EXCHANGER IS NOT VALID ; { 1, 0 } --> L2_Begin >>>>>
            }

            OutDev(String.Format(">>>>>>> zChess \"{0}\" [Save As/Confirmation] <<<<<<<\n\r", inputName));

            string fileName = inputName + ".zus";
            fileName = Path.Combine(DefaultPath, fileName);

            if (!File.Exists(fileName) || input == "yes")
            {
                string errStr = SaveGame(fileName, Gamestuff.Scoresheet);

                if (errStr == "")
                {
                    LastSaveName = fileName;
                    gameName = inputName;
                    Exchanger = null;

                    OutDev(String.Format(" >: \"{0}\" was successfully saved\n\n\r >>> Press Enter to resume the game <<<", inputName));
                    InDev();   
                }
                else
                {
                    OutDev(String.Format(" >: Unable to save:\n\r{0}\n\r >>> Press Enter to resume the game <<<", errStr));
                    InDev();
                }

                address = new byte[2] { 1, 0 }; // --> L2_Begin
            }
            else if (input == "no")
            {
                address = new byte[3] { 1, 0, 0 }; // --> L3_SaveAs
                Exchanger = null;
            }
            else if (input == "cancel")
            {
                address = new byte[2] { 1, 0 }; // --> L2_Begin
                Exchanger = null;
            }
            else
            {
                OutDev(String.Format(" >: \"{0}\" is already exists. Do you want to replace it?\n\r", inputName));
                OutDev("no     - change filename");
                OutDev("yes    - save and resume the game");
                OutDev("cancel - don't save and resume the game");
                OutDev("-------");
            }

            return address;
        }

        /// <summary>
        /// Promotion ; 
        /// L3_Promotion address: { 1, 0, 2 }
        /// </summary>
        static byte[] L3_Promotion(string input, Inputs InDev, Outputs OutDev)
        {
            byte[] address = null;

            string[] UpPiecesMenu = new string[5] { "", "Queen", "Rook", "Bishop", "Knight" };
            string[] LoPiecesMenu = new string[5] { "", "queen", "rook", "bishop", "knight" };
            int index = -1;

            for (int i = 1; i < 5; i++)
            {
                if (input.Length > 0 && (input == UpPiecesMenu[i] || input == LoPiecesMenu[i] ||
                    input.Substring(0,1) == UpPiecesMenu[i].Substring(0,1) ||
                    input.Substring(0,1) == LoPiecesMenu[i].Substring(0,1)) )
                {
                    index = i;
                    break;
                }
            }

            if (index > 0)
            {
                sbyte[] moveVector = Exchanger as sbyte[];

                if (moveVector != null)
                {
                    if (PlayerColor == Colors.nil)
                    {
                        chessman Owner = Gamestuff.Require(moveVector[2], moveVector[3]);
                        Colors color = Owner.color;

                        Gamestuff.Addpiece(color, (Pieces)index, moveVector[2], moveVector[3], promotion: true);
                    }
                    else
                    {
                        Gamestuff.Addpiece(PlayerColor, (Pieces)index, moveVector[2], moveVector[3], promotion: true);

                        PlayerStroke = false;
                    }

                    Gamestuff.Scoresheet.Promotion((Pieces)index);
                    Gamestuff.Scoresheet.Write(useTmp: true);

                    Exchanger = null;
                    address = new byte[2] { 1, 0 }; // L2_Begin
                }
                else
                {
                    address = new byte[4] { 1, 0, 2, 0 }; // L3_internalError
                }    
            }
            else // show menu items
            {
                OutDev(String.Format(">>>>>>> zChess \"{0}\" [Promotion] <<<<<<<", gameName));
                OutDev(" Select piece to replace:\n\r");

                for (int i = 1; i < 5; i++)
                {
                    OutDev( String.Format("{0} - {1}", LoPiecesMenu[i].Substring(0, 1), UpPiecesMenu[i]) );
                }

                OutDev("-------");
            }

            return address;
        }

        /// <summary>
        /// L3_Exit address: { 1, 0, 4 }
        /// </summary>
        static byte[] L3_Exit(string input, Inputs _d, Outputs device)
        {
            device(String.Format(">>>>>>> zChess \"{0}\" [Exit] <<<<<<<\r\n", gameName));

            string comments = "";

            if (input == "y" || input == "Y" || input == "yes")
            {
                if (LastSaveName != "")
                {
                    string errStr = SaveGame(LastSaveName, Gamestuff.Scoresheet);

                    if (errStr == "")
                    {
                        device(" Game was successfully saved\n\r");

                        device(" >>> Press Enter to go home menu <<< ");
                        _d();

                        return new byte[1] { 0 }; // >>>>> --> L0_Home >>>>>
                    }
                    else
                    {
                        comments = "Unable to save the game\n\r";
                    }
                }
                else
                {
                    return new byte[3] { 1, 0, 0 }; // >>>>> --> L3_SaveAs >>>>>
                }
            }
            else if (input == "n" || input == "N" || input == "no")
            {
                return new byte[1] { 0 }; // >>>>> --> L0_Home >>>>>
            }
            else if (input == "c" || input == "C" || input == "cancel")
            {
                return new byte[2] { 1, 0 }; // >>>>> --> L2_Begin >>>>>
            }

            if (comments == "")
            {
                device(" Save the game before exit?\n\r");
                device("yes    - save");
            } 
            else
            {
                device(comments);
            }
            
            device("no     - home");
            device("cancel - resume game");
            device("-------");

            return null;
        }

        /// <summary>
        /// L1_Exit address: { 3 }
        /// </summary>
        static byte[] L1_Exit(string input, Inputs _d, Outputs device)
        {
            if (input == "y" || input == "Y" || input == "yes")
            {
                return new byte[1] { 255 };
            }
            else if (input == "n" || input == "N" || input == "no")
            {
                return new byte[1] { 0 };
            }

            device(">>>>>>> zChess [Exit] <<<<<<<\r\n");
            device("yes - exit");
            device("no  - home");
            device("-------");

            return null;
        }

        // end of Menus handlers ///////////////////////////////////////////////////////////////////

        static void Main(string[] args)
        {
            Inputs InDevice = Console.ReadLine;
            Outputs OutDevice = Console.WriteLine;
            //Console.TreatControlCAsInput = true; // ? 

            // Checking of permissions for Read/Write
            IOexceptionStr = "";

            try
            {
                DefaultPath = Directory.GetCurrentDirectory();

                FileIOPermission permitRW = new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.Write, DefaultPath);
                permitRW.Demand();
            }
            catch (SecurityException SE)
            {
                IOexceptionStr = SE.ToString();
                IOexceptionFlag = true;
            }
            catch (Exception E)
            {
                IOexceptionStr += "\n\r-------------------------------------\n\r";
                IOexceptionStr += E.ToString();
                IOexceptionFlag = true;
            }

            if (IOexceptionFlag)
            {
                Console.WriteLine(">>>>>>> zChess [WARNING!] <<<<<<<\r\n");
                Console.WriteLine("You will not be able to save/open games due to:\n\r");
                Console.WriteLine(IOexceptionStr);

                Console.WriteLine("\n\r>>> Press any key to continue <<<");
                Console.ReadKey();
            }
            else
            {
                DefaultPath = Path.Combine(DefaultPath, "SavedGames");

                if (!Directory.Exists(DefaultPath))
                {
                    Directory.CreateDirectory(DefaultPath);
                }
            }

            // NavDirectory[] and Navigators[] ----------------------------------------------------------
            // Address hierarchy ex.:
            // L0:                          Home {0}
            // L1:           {1},                {2},                    {3}
            // L2:      {1, 0}, {1, 1},     {2, 0}, {2, 1},    {3, 0}, {3, 1}, {3, 2}
            //
            // All addresses and corresponding Navigators will be sorted by means of the method Sort()
            Navigators = new Interactor[9];
            NavDirectory = new byte[9][];

            NavDirectory[0] = new byte[1] { 0 };            Navigators[0] = L0_Home;
            NavDirectory[1] = new byte[1] { 3 };            Navigators[1] = L1_Exit;
            NavDirectory[2] = new byte[1] { 1 };            Navigators[2] = L1_New;
            NavDirectory[3] = new byte[2] { 1, 0 };         Navigators[3] = L2_Begin;
            NavDirectory[4] = new byte[3] { 1, 0, 2 };      Navigators[4] = L3_Promotion;
            NavDirectory[5] = new byte[3] { 1, 0, 0 };      Navigators[5] = L3_SaveAs;
            NavDirectory[6] = new byte[4] { 1, 0, 0, 0 };   Navigators[6] = L4_SaveAsCnfrm;
            NavDirectory[7] = new byte[1] { 2 };            Navigators[7] = L1_Open;
            NavDirectory[8] = new byte[3] { 1, 0, 4 };      Navigators[8] = L3_Exit;
            // end of NavDirectory[] and Navigators[] ------------------------------------------------------

            int index = 0;
            byte[] address = null;
            byte[] addressLast = new byte[1] { 0 };
            string input = "";
            Interactor Shunter = L0_Home;

            if ( !Sort() )
            {
                address = new byte[1] { 255 };
                Console.WriteLine("zChess:> STOP exception : Sort : LENGTHS OF DIRECTORY AND NAVIGATORS ARE NOT EQUAL");
            }

            while (address == null || address[0] != 255) // BROWSING ENTRANCE -------------------------------
            {
                Console.Clear();

                address = Shunter(input, InDevice, OutDevice);

                if (address != null)
                {
                    if (address[0] != 255)
                    {
                        Shunter = Search(address, out index);

                        Console.Clear();

                        if (index == -1) // non-existent address
                        {
                            Console.WriteLine("zChess:> missing menu item");
                            Console.WriteLine(">>> Press any key to continue <<<");
                            Console.ReadKey();

                            Shunter = Search(addressLast, out index);
                        }
                        else
                        {
                            addressLast = address;   
                        }

                        input = ""; // to show a new menu items
                    }
                }
                else
                {
                    input = Console.ReadLine();
                }
            } // BROWSING EXIT -------------------------------------------------------------------------------

            Console.WriteLine(">>>>>>> Press any key to exit <<<<<<<");
            Console.ReadKey();
        }
    }
}
