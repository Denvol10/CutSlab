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

        public CuttingSolid() 
        {
            BeamToplines = new List<DirectShape>();
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
