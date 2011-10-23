using System.Collections.Generic;

using Zepheus.Zone.Handlers;

namespace Zepheus.Zone.Game
{
    public delegate void QuestionCallback(ZoneCharacter character, byte answer);

    public sealed class Question
    {
        public string Text { get; private set; }
        public QuestionCallback Function { get; private set; }
        public List<string> Answers { get; private set; }
        public object Object { get; set; }

        public Question(string pText, QuestionCallback pFunction, object obj = null)
        {
            this.Text = pText;
            Function = pFunction;
            Answers = new List<string>();
            Object = obj;
        }

        public void Add(params string[] text)
        {
            Answers.AddRange(text);
        }

        public void Send(ZoneCharacter character, ushort distance = 1000)
        {
            Handler15.SendQuestion(character, this, distance);
        }
    }
}
