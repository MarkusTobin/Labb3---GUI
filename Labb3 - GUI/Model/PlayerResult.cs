﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Labb3___GUI.Model
{
    internal class PlayerResult
    {

        public Object Id { get; set; }
        public string PlayerName { get; set; }
        public Object QuestionPackId { get; set; }
        public string QuestionPackName { get; set; }
        public int TotalCorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public TimeSpan TotalTime { get; set; }
        public DateTime DatePlayed { get; set; }
        public List <PlayerAnswer> Answers { get; set; }
    }

    internal class PlayerAnswer
    {
        public string QuestionText { get; set; }
        public string SelectedAnswer { get; set; }
        public string CorrectAnswer { get; set; }
        public bool IsCorrect => SelectedAnswer == CorrectAnswer;
    }
}
