using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WvsBeta.Game.GameObjects.DataLoading
{
    public class QuizData
    {
        public readonly byte QuestionPage;
        public readonly byte QuestionIdx;
        public readonly char Answer;

        public QuizData(byte questionPage, byte questionIdx, char answer)
        {
            QuestionPage = questionPage;
            QuestionIdx = questionIdx;
            Answer = answer;
        }
    }
}
