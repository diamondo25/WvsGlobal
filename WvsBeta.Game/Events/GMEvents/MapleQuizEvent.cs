using System.Collections.Generic;
using System.Linq;
using WvsBeta.Game.GameObjects.DataLoading;
using static WvsBeta.MasterThread;
using WvsBeta.Common;
using System.Drawing;

namespace WvsBeta.Game.Events.GMEvents
{
    class MapleQuizEvent : Event
    {
        private static readonly int QuizMapId = 109020001;
        private static readonly Map QuizMap = DataProvider.Maps[QuizMapId];
        private static readonly int WinMapId = 109050000;
        private static readonly int LoseMapId = 109050001;
        private static readonly Rectangle AreaO = new Rectangle(-1030, -150, 780, 510);
        private static readonly Rectangle AreaX = new Rectangle(-210, -150, 780, 510);

        private RepeatingAction curQuestion;
        private List<QuizData> questions; //page, index, answer. 0 = x, 1 = o for the answer

        public MapleQuizEvent()
        {
            questions = new List<QuizData>();
        }

        public override void Prepare()
        {
            questions.Clear();
            
            var page = DataProvider.QuizQuestions[(byte)(1 + Rand32.Next() % 7)];
            while(questions.Count < 10)
            {
                var nextQuestion = page.RandomElement();
                if (!questions.Contains(nextQuestion))
                    questions.Add(nextQuestion);
            }

            QuizMap.ChatEnabled = true;
            QuizMap.PortalsOpen = false;
            base.Prepare();
        }

        public override void Join(Character chr)
        {
            base.Join(chr);
            chr.ChangeMap(QuizMapId, "start00");
        }

        public override void Start(bool joinDuringEvent = false)
        {
            QuizMap.ChatEnabled = false;
            base.Start(joinDuringEvent);
            AskQuestion();
        }

        private void AskQuestion()
        {
            QuizData Question = questions.Last();
            Program.MainForm.LogDebug("Asking question.... Answer: " + Question.Answer);
            questions.RemoveAt(questions.Count - 1);
            QuizMap.SendPacket(OXPackets.QuizQuestion(true, Question.QuestionPage, Question.QuestionIdx));
            curQuestion = RepeatingAction.Start("Quiz - " + (questions.Count - 1) + " - question", t => CheckAnswer(Question), 30*1000, 0);
        }

        private void CheckAnswer(QuizData question)
        {
            QuizMap.SendPacket(OXPackets.QuizQuestion(false, question.QuestionPage, question.QuestionIdx));

            var losers = QuizMap.Characters
                                .Where(
                                    chr => chr.Foothold < 0 ||
                                    (!AreaO.Contains(chr.Position.X, chr.Position.Y) && !AreaX.Contains(chr.Position.X, chr.Position.Y)) ||
                                    (AreaO.Contains(chr.Position.X, chr.Position.Y) && question.Answer != 'o') ||
                                    (AreaX.Contains(chr.Position.X, chr.Position.Y) && question.Answer != 'x'))
                                .ToList();
            losers.ForEach(c => c.ChangeMap(LoseMapId));

            if (questions.Count == 0)
            {
                Stop();
            }
            else
            {
                Program.MainForm.LogDebug("Asking next question...");
                curQuestion = RepeatingAction.Start("Quiz - " + (questions.Count - 1) + " - answer", t => AskQuestion(), 10 * 1000, 0);
            }
        }

        public override void Stop()
        {
            curQuestion?.Stop();
            curQuestion = null;
            EventHelper.WarpEveryone(QuizMap, WinMapId);
            QuizMap.ChatEnabled = true;
            QuizMap.PortalsOpen = true;
            base.Stop();
        }

        public void StopEarly()
        {
            curQuestion?.Stop();
            curQuestion = null;
            EventHelper.WarpEveryone(QuizMap, LoseMapId);
            QuizMap.ChatEnabled = true;
            QuizMap.PortalsOpen = true;
            base.Stop();
        }
    }
}
