using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Voting.Models
{
    public class Category:INotifyPropertyChanged
    {
        public ObservableCollection<Candidate> Candidates { get; set; }
        public string CatName { get; set; }
        public Int64 Id { get; set; }
        private int maxVote;
        private int countSelected;
        public int MaxVote { get { return maxVote; } set { maxVote = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MaxVote")); } }
        public int CountSelected { get { return countSelected; } set { countSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CountSelected")); } }

        public Category(string name)
        {
            Candidates = new ObservableCollection<Candidate>();
            CatName = name;
            CountSelected = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return CatName;
        }
    }
}
