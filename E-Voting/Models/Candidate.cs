﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Voting.Models
{
    public class Candidate:INotifyPropertyChanged
    {
        private string candidateName;
        private Int64 id;
        private string imageUri;
        private bool selected;
        private bool voted;

        public event PropertyChangedEventHandler PropertyChanged;

        public string CandidateName { get { return candidateName; } set { candidateName = value; } }
        public Int64 UniqueId { get { return id; } set { id = value; } }
        public string ImageUri { get { return imageUri; } set { imageUri = value; } }
        public bool Selected { get { return selected; } set { selected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Selected")); } }
        public bool Voted { get { return voted; } set { voted = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Voted")); } }
        public int CatId { get; set; }

        public Candidate()
        {
            Selected = false;
            Voted = false;
        }

        public Candidate(string name, Int64 id)
        {
            candidateName = name;
            this.id = id;
            Selected = false;
            Voted = false;
        }

    }
}
