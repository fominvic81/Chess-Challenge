using Raylib_cs;
using System.Numerics;
using System;
using System.IO;

namespace ChessChallenge.Application
{
    public static class MenuUI
    {
        public static void DrawButtons(ChallengeController controller)
        {

            Vector2 buttonPos = UIHelper.Scale(new Vector2(150, 210));
            Vector2 buttonSize = UIHelper.Scale(new Vector2(200, 55));
            float spacing = buttonSize.Y * 1.2f;
            float breakSpacing = spacing * 0.6f;

            // Game Buttons
            if (NextButtonInRow("Human vs MyBot", ref buttonPos, spacing, buttonSize))
            {
                var whiteType = controller.HumanWasWhiteLastGame ? ChallengeController.PlayerType.MyBot : ChallengeController.PlayerType.Human;
                var blackType = !controller.HumanWasWhiteLastGame ? ChallengeController.PlayerType.MyBot : ChallengeController.PlayerType.Human;
                controller.StartNewMatch(whiteType, blackType);
            }
            if (NextButtonInRow("MyBot vs MyBot", ref buttonPos, spacing, buttonSize))
            {
                controller.StartNewMatch(ChallengeController.PlayerType.MyBot, ChallengeController.PlayerType.MyBot);
            }
            if (NextButtonInRow("MyBot vs EvilBot", ref buttonPos, spacing, buttonSize))
            {
                controller.StartNewMatch(ChallengeController.PlayerType.MyBot, ChallengeController.PlayerType.EvilBot);
            }

            // Page buttons
            buttonPos.Y += breakSpacing;

            if (NextButtonInRow("Save Games", ref buttonPos, spacing, buttonSize))
            {
                string pgns = controller.AllPGNs;
                string directoryPath = Path.Combine(FileHelper.AppDataPath, "Games");
                Directory.CreateDirectory(directoryPath);
                string fileName = FileHelper.GetUniqueFileName(directoryPath, "games", ".txt");
                string fullPath = Path.Combine(directoryPath, fileName);
                File.WriteAllText(fullPath, pgns);
                ConsoleHelper.Log("Saved games to " + fullPath, false, ConsoleColor.Blue);
            }
            if (NextButtonInRow("Rules & Help", ref buttonPos, spacing, buttonSize))
            {
                FileHelper.OpenUrl("https://github.com/SebLague/Chess-Challenge");
            }
            if (NextButtonInRow("Documentation", ref buttonPos, spacing, buttonSize))
            {
                FileHelper.OpenUrl("https://seblague.github.io/chess-coding-challenge/documentation/");
            }
            if (NextButtonInRow("Submission Page", ref buttonPos, spacing, buttonSize))
            {
                FileHelper.OpenUrl("https://forms.gle/6jjj8jxNQ5Ln53ie6");
            }

            // Window and quit buttons
            buttonPos.Y += breakSpacing;

            bool isBigWindow = Raylib.GetScreenWidth() > Settings.ScreenSizeSmall.X;
            string windowButtonName = isBigWindow ? "Smaller Window" : "Bigger Window";
            if (NextButtonInRow(windowButtonName, ref buttonPos, spacing, buttonSize))
            {
                Program.SetWindowSize(isBigWindow ? Settings.ScreenSizeSmall : Settings.ScreenSizeBig);
            }
            if (NextButtonInRow("Exit (ESC)", ref buttonPos, spacing, buttonSize))
            {
                Environment.Exit(0);
            }
            if (NextButtonInRow("Fast forward", ref buttonPos, spacing, buttonSize))
            {
                controller.fastForward = !controller.fastForward;
            }

            buttonPos = UIHelper.Scale(new Vector2(405, 210));
            buttonSize = UIHelper.Scale(new Vector2(200, 55));
            if (NextButtonInRow("Human vs PrevBot", ref buttonPos, spacing, buttonSize))
            {
                var whiteType = controller.HumanWasWhiteLastGame ? ChallengeController.PlayerType.MyBot : ChallengeController.PlayerType.Human;
                var blackType = !controller.HumanWasWhiteLastGame ? ChallengeController.PlayerType.PrevBot : ChallengeController.PlayerType.Human;
                controller.StartNewMatch(whiteType, blackType);
            }
            if (NextButtonInRow("MyBot vs NegamaxV1", ref buttonPos, spacing, buttonSize))
            {
                controller.StartNewMatch(ChallengeController.PlayerType.MyBot, ChallengeController.PlayerType.NegamaxV1);
            }
            if (NextButtonInRow("MyBot vs PrevBot", ref buttonPos, spacing, buttonSize))
            {
                controller.StartNewMatch(ChallengeController.PlayerType.MyBot, ChallengeController.PlayerType.PrevBot);
            }

            bool NextButtonInRow(string name, ref Vector2 pos, float spacingY, Vector2 size)
            {
                bool pressed = UIHelper.Button(name, pos, size);
                pos.Y += spacingY;
                return pressed;
            }
        }
    }
}
