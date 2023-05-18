using AWK;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;

namespace AwkwardMP.Patches
{
    internal class ScoreEntry
    {
        public int Index;

        public int ScorePercentage;

        public string FirstDisplayName;
    }

    internal class ScoreEntryComparer : IComparer<ScoreEntry>
    {
        public int Compare(ScoreEntry x, ScoreEntry y)
        {
            if (x.ScorePercentage == y.ScorePercentage)
            {
                return new CaseInsensitiveComparer().Compare(x.FirstDisplayName, y.FirstDisplayName);
            }

            if (x.ScorePercentage > y.ScorePercentage)
            {
                return -1;
            }

            return 1;
        }
    }


    /* ***************************************
     * 
     *  Score Display Patch
     * 
     *****************************************/

    [HarmonyPatch(typeof(ScoreDisplay))]
    internal class ScoreDisplayPatch
    {
        private static ScoreDisplay Instance = null;

        private static int m_AverageScorePercentage = 0;


        private static List<ScoreEntry> m_scoreEntryList = new List<ScoreEntry>();

        private static List<int> m_usedRandomIndices = new List<int>();

        [HarmonyPostfix]
        [HarmonyPatch("Show")]
        private static void PostfixShow(ScoreDisplay __instance, bool isEndOfGame)
        {
            
            if (Instance == null)
                Instance = __instance;

            m_AverageScorePercentage = 0;

            switch (Globals.GameState.CurrentGameType)
            {
                case GameState.GameType.Solo:
                    ShowForSoloMode(isEndOfGame);
                    break;
                case GameState.GameType.SoloDuo:
                    ShowForSoloDuo(isEndOfGame);
                    break;
                case GameState.GameType.Matchup:
                    ShowForMatchup(isEndOfGame);
                    break;
                case GameState.GameType.MultipleDuo:
                    ShowForMultipleDuo(isEndOfGame);
                    break;
                case GameState.GameType.LiveShow:
                    ShowForLiveShow(isEndOfGame);
                    break;
            }
        }


        private static void ShowForSoloMode(bool isEndOfGame)
        {
            int num = CalculateScorePercentageGeneral();
            m_AverageScorePercentage = num;

            AwkwardClient.ShowScore(m_AverageScorePercentage, new { }, isEndOfGame);
        }

        private static void ShowForSoloDuo(bool isEndOfGame)
        {
            int num = CalculateScorePercentageGeneral();
            m_AverageScorePercentage = num;
       
            AwkwardClient.ShowScore(m_AverageScorePercentage, new { }, isEndOfGame);
        }

        private static void ShowForMatchup(bool isEndOfGame)
        {
            m_scoreEntryList.Clear();
            for (int i = 0; i < Globals.GameState.NumberOfPlayers; i++)
            {
                int num = 0;
                int num2 = 0;
                CalculatePlayerScore(i, out num2, out num);

                int scorePercentage = Globals.GameState.GetScorePercentage((double)num2, (double)num);
                string playerName = Globals.GameState.GetPlayerName(i);
                m_scoreEntryList.Add(new ScoreEntry
                {
                    Index = i,
                    ScorePercentage = scorePercentage,
                    FirstDisplayName = playerName
                });
                m_AverageScorePercentage += scorePercentage;
            }

            m_AverageScorePercentage /= m_scoreEntryList.Count;
            m_scoreEntryList.Sort(new ScoreEntryComparer());

            AwkwardClient.ShowScore(m_AverageScorePercentage, new { scores = m_scoreEntryList }, isEndOfGame);
        }

        private static void ShowForMultipleDuo(bool isEndOfGame)
        {
            for (int i = 0; i < Globals.GameState.GetNumberOfTeams(); i++)
            {
                int num = 0;
                int num2 = 0;
                CalculateTeamScores(i, out num2, out num);

                int scorePercentage = Globals.GameState.GetScorePercentage((double)num2, (double)num);
                string teamPlayerName = Globals.GameState.GetTeamPlayerName(i, 0);
                
                m_scoreEntryList.Add(new ScoreEntry
                {
                    Index = i,
                    ScorePercentage = scorePercentage,
                    FirstDisplayName = teamPlayerName
                });
                m_AverageScorePercentage += scorePercentage;
            }

            m_AverageScorePercentage /= m_scoreEntryList.Count;
            m_scoreEntryList.Sort(new ScoreEntryComparer());

            AwkwardClient.ShowScore(m_AverageScorePercentage, new { scores = m_scoreEntryList }, isEndOfGame);
        }

        private static void ShowForLiveShow(bool isEndOfGame)
        {
            m_scoreEntryList.Clear();
            m_AverageScorePercentage = 0;

            Dictionary<string, TextVoteParser.Viewer> viewers = Globals.StreamingServices.ViewerVoteCollector.GetViewers();
            foreach (KeyValuePair<string, TextVoteParser.Viewer> keyValuePair in viewers)
            {
                TextVoteParser.Viewer value = keyValuePair.Value;
                int num = 0;
                int num2 = 0;
                CalculateViewerScore(value, out num2, out num);
                int scorePercentage = Globals.GameState.GetScorePercentage((double)num2, (double)num);
                string name = value.Name;
                m_scoreEntryList.Add(new ScoreEntry
                {
                    Index = 0,
                    ScorePercentage = scorePercentage,
                    FirstDisplayName = name
                });
                m_AverageScorePercentage += scorePercentage;
            }

            m_scoreEntryList.Sort(new ScoreEntryComparer());
            if (m_scoreEntryList.Count > 0)
            {
                m_AverageScorePercentage /= m_scoreEntryList.Count;
            }

            AwkwardClient.ShowScore(m_AverageScorePercentage, new { scores = m_scoreEntryList }, isEndOfGame);
        }


        private static int CalculateScorePercentageGeneral()
        {
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            int num4 = 0;
            List<QuestionState> questions = Globals.GameState.GetQuestions();
            foreach (QuestionState questionState in questions)
            {
                if (!questionState.HasBeenAnswered())
                {
                    break;
                }
                num++;
                num3 += questionState.Difficulty;
                if (questionState.WasAnsweredCorrectly())
                {
                    num2++;
                    num4 += questionState.Difficulty;
                }
            }
            return Globals.GameState.GetScorePercentage((double)num4, (double)num3);
        }

        private static void CalculateTeamScores(int teamIndex, out int weightedTotalCorrectAnswers, out int weightedTotalQuestions)
        {
            weightedTotalQuestions = 0;
            weightedTotalCorrectAnswers = 0;
            List<QuestionState> questions = Globals.GameState.GetQuestions();
            foreach (QuestionState questionState in questions)
            {
                if (!questionState.HasBeenAnswered())
                {
                    break;
                }
                if (questionState.TeamIndex == teamIndex)
                {
                    weightedTotalQuestions += questionState.Difficulty;
                    if (questionState.WasAnsweredCorrectly())
                    {
                        weightedTotalCorrectAnswers += questionState.Difficulty;
                    }
                }
            }
        }

        private static void CalculatePlayerScore(int playerIndex, out int weightedTotalCorrectAnswers, out int weightedTotalQuestions)
        {
            weightedTotalQuestions = 0;
            weightedTotalCorrectAnswers = 0;
            List<QuestionState> questions = Globals.GameState.GetQuestions();
            foreach (QuestionState questionState in questions)
            {
                if (!questionState.HasBeenAnswered())
                {
                    break;
                }
                if (questionState.ChoosingPlayerIndex == playerIndex || questionState.GuessingPlayerIndex == playerIndex)
                {
                    weightedTotalQuestions += questionState.Difficulty;
                    if (questionState.WasAnsweredCorrectly())
                    {
                        weightedTotalCorrectAnswers += questionState.Difficulty;
                    }
                }
            }
        }

        private static void CalculateViewerScore(TextVoteParser.Viewer viewer, out int weightedTotalCorrectAnswers, out int weightedTotalQuestions)
        {
            weightedTotalQuestions = 0;
            weightedTotalCorrectAnswers = 0;
            List<QuestionState> questions = Globals.GameState.GetQuestions();
            int count = questions.Count;
            for (int i = 0; i < count; i++)
            {
                QuestionState questionState = questions[i];
                if (!questionState.HasBeenAnswered())
                {
                    break;
                }
                weightedTotalQuestions += questionState.Difficulty;
                if (viewer.IsCorrect(i, questionState.ChoosingPlayerAnswerID))
                {
                    weightedTotalCorrectAnswers += questionState.Difficulty;
                }
            }
        }
    }


    /************************************************
     * 
     * InGame Scene Logic 
     * 
     ************************************************/

    [HarmonyPatch(typeof(InGameSceneLogic))]
    internal class IngameSceneLogicPatch
    {
        // Do Score Display ( ScoreDisplay.Show ) if m_shouldEndLiveShow
        // Do Round Bumper
        // Do Turn Announcement
        // Do Question 
        // Do Answer Reveal
        // Do Question Stats Display

        [HarmonyPostfix]
        [HarmonyPatch("Initialise")]
        private static void PostfixInitialise()
        {
            AwkwardClient.FixPlayerNames();
            AwkwardClient.SetMaxPlayers(Globals.GameState.NumberOfPlayers);
        }

        [HarmonyPostfix]
        [HarmonyPatch("DoRoundBumper")]
        private static void PostfixDoRoundBumper(IEnumerator __result)
        {
            QuestionState question = Globals.GameState.GetCurrentQuestion();
            AwkwardClient.ShowNextRound(question.RoundIndex);
        }

        [HarmonyPostfix]
        [HarmonyPatch("DoTurnAnnouncement")]
        private static void PostfixDoTurnAnnouncement(bool isChoosing)
        {
            string choosingPlayerName = Globals.GameState.GetChoosingPlayerNameForCurrentQuestion();
            string guessingPlayerName = Globals.GameState.GetGuessingPlayerNameForCurrentQuestion();

            AwkwardClient.StartNextTurn(isChoosing, choosingPlayerName, guessingPlayerName);
        }

        [HarmonyPostfix]
        [HarmonyPatch("DoQuestion")]
        private static void PostfixDoQuestion(IEnumerator __result, bool isChoosing, bool wantFadeToBlackAtEnd)
        {
            int playerIndex = isChoosing ? Globals.GameState.GetCurrentQuestion().ChoosingPlayerIndex : Globals.GameState.GetCurrentQuestion().GuessingPlayerIndex;

            object question = Helper.CurrentQuestion();

            AwkwardClient.BroadcastQuestion(question, playerIndex, isChoosing);
        }


        [HarmonyPostfix]
        [HarmonyPatch("DoAnswerReveal")]
        private static void PostfixDoAnswerReveal(IEnumerator __result)
        {
            QuestionState question = Globals.GameState.GetCurrentQuestion();
            int chosenAnswerID = question.ChoosingPlayerAnswerID;
            int guessedAnswerID = question.GuessingPlayerAnswerID;
            bool isCorrect = chosenAnswerID == guessedAnswerID;


            AwkwardClient.RevealAnswer(chosenAnswerID, guessedAnswerID, isCorrect);
        }


        [HarmonyPostfix]
        [HarmonyPatch("DoQuestionStatsDisplay")]
        private static void PostfixDoQuestionStatsDisplay(IEnumerator __result)
        {
            QuestionState currentQuestion = Globals.GameState.GetCurrentQuestion();
            AwkwardClient.AnnounceStats(currentQuestion.Answer1Percentage, currentQuestion.Answer2Percentage);
        }
    }
}
