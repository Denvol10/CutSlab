using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CutSlab.Models
{
    public class CuttingSolid : INotifyPropertyChanged
    {
        #region Верх балок
        private string _beamTopLinesIds;
        public string BeamTopLinesIds
        {
            get => _beamTopLinesIds; 
            set
            {
                _beamTopLinesIds = value;
                OnPropertyChanged("BeamTopLinesIds");
            }
        }

        private List<DirectShape> _beamTopLines;

        public List<DirectShape> BeamToplines 
        {
            get => _beamTopLines; 
            set
            {
                _beamTopLines = value;
            }
        }
        #endregion

        #region Границы подрезки
        private string _boundLinesIds;
        public string BoundLinesIds
        {
            get => _boundLinesIds;
            set
            {
                _boundLinesIds = value;
                OnPropertyChanged("BoundLinesIds");
            }
        }

        private List<ModelLine> _boundLines;

        public List<ModelLine> BoundLines
        {
            get => _boundLines;
            set
            {
                _boundLines = value;
            }
        }
        #endregion

        public CuttingSolid() 
        {
            BeamToplines = new List<DirectShape>();
            BoundLines = new List<ModelLine>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        // Метод получения строки с ElementId
        private void ElementIdToString()
        {
            var stringArr = _beamTopLines.Select(e => "Id" + e.Id.IntegerValue.ToString()).ToArray();
            string resultString = string.Join(", ", stringArr);
            BeamTopLinesIds = resultString;
        }
    }
}
