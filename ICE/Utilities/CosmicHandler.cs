using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Utilities
{
    internal class CosmicHandler
    {
        internal static HashSet<string> commenceStrings = ["Commence selected mission?",
                                                           "Ausgewählte Mission wird gestartet.Fortfahren?",
                                                           "Commencer la mission sélectionnée ?",
                                                           "選択したミッションを開始します。よろしいですか？",
                                                           "确定要开始此任务吗？", "確定要開始此任務嗎？", "선택한 임무를 시작하시겠습니까?"
                                                          ];

        internal static HashSet<string> abandonStrings = ["Abandon mission?",            // English
                                                          "Aktuelle Mission abbrechen?", // German
                                                          "Êtes-vous sûre de vouloir abandonner la mission en cours ?", // French
                                                          "受注中のミッションを破棄します。", // Japanese
                                                          "确定要放弃已领取的任务吗？",      // Chinese
                                                          "確定要放棄已領取的任務嗎？",
                                                          "수락한 임무를 포기하시겠습니까?"  // Korean?
                                                          ]; 

        internal unsafe static bool IsMissionTimedOut()
        {
            if (AddonHelper.GetAtkTextNode("WKSMissionInfomation", 23)->IsVisible())
                return true;
            else
                return false;
        }
        internal unsafe static (int classScore, int cappedClassScore, int totalScores, uint classId) GetCosmicClassScores()
        {
            int classScore = 0;
            int cappedClassScore = 0;
            int totalScores = 0;
            var wksManager = WKSManager.Instance();
            var currentMissionId = wksManager->CurrentMissionUnitRowId;

            uint classId;
            /*
             * Unsure how to handle this right now... doesn't work with dual classes. Might need to just generally go back and figure out how to view this
            if (currentMissionId > 0 &&
                CosmicHelper.SheetMissionDict.TryGetValue(currentMissionId, out var missionInfo))
                classId = missionInfo.JobId;
            else
            */
            classId = (uint)(Svc.ClientState.LocalPlayer?.ClassJob.RowId);

            if (classId is >= 8 and <= 18)
            {
                var scores = wksManager->Scores;

                classScore = scores[(int)classId - 8];
                cappedClassScore = Math.Min(500_000, classScore);

                totalScores = 0;
                for (int i = 0; i < scores.Length; ++i)
                    totalScores += Math.Min(500_000, scores[i]);
            }


            return (classScore, cappedClassScore, totalScores, classId);
        }

        internal unsafe static (int currentScore, int silverScore, int GoldScore) CurrentScore()
        {
            int currentScore = 0;
            int silverScore = 0;
            int GoldScore = 0;

            return (currentScore, silverScore, GoldScore);
        }


    }
}
