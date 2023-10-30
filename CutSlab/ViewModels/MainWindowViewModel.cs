using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CutSlab.Infrastructure;
using CutSlab.Models;
using System.IO;

namespace CutSlab.ViewModels
{
    internal class MainWindowViewModel : Base.ViewModel
    {
        private RevitModelForfard _revitModel;

        internal RevitModelForfard RevitModel
        {
            get => _revitModel;
            set => _revitModel = value;
        }

        #region Заголовок
        private string _title = "Подрезать плиту снизу";

        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }
        #endregion

        #region Список тел для подрезки
        private ObservableCollection<CuttingSolid> _cuttingSolidsCollection;
        public ObservableCollection<CuttingSolid> CuttingSolidsCollection
        {
            get => _cuttingSolidsCollection;
            set => Set(ref _cuttingSolidsCollection, value);
        }
        #endregion

        #region Команды

        #region Добавление тела подрезки
        public ICommand AddCutSolidCommand { get; }

        private void OnAddCutSolidCommandExecuted(object parameter)
        {
            var newCutSolid = new CuttingSolid();

            CuttingSolidsCollection.Add(newCutSolid);
        }

        private bool CanAddCutSolidCommandExecute(object parameter)
        {
            return true;
        }
        #endregion

        #region Удаление тела подрезки из списка
        public ICommand RemoveCutSolidCommand { get; }

        private void OnRemoveCutSolidCommandExecuted(object parameter)
        {
            var lastCutSolid = CuttingSolidsCollection.LastOrDefault();

            if(!(lastCutSolid is null))
            {
                CuttingSolidsCollection.Remove(lastCutSolid);
            }
        }

        private bool CanRemoveCutSolidCommandExecute(object parameter)
        {
            if (CuttingSolidsCollection.LastOrDefault() is null)
            {
                return false;
            }

            return true;
        }
        #endregion

        #region Выбрать линии верха балок
        public ICommand SelectTopLinesCommand { get; }

        private void OnSelectTopLinesCommandExecuted(object parameter)
        {
            RevitCommand.mainView.Hide();
            CuttingSolid cuttingSolid = parameter as CuttingSolid;
            cuttingSolid.BeamToplines = RevitModel.GetTopLines();
            cuttingSolid.BeamTopLinesIds = RevitModel.ElementIdToString(cuttingSolid.BeamToplines);
            RevitCommand.mainView.ShowDialog();
        }

        private bool CanSelectTopLinesCommandExecute(object paramter)
        {
            return true;
        }
        #endregion

        #region Выбрать границы подрезки
        public ICommand SelectBoundLinesCommand { get; }

        private void OnSelectBoundLinesCommandExecuted(object parameter)
        {
            RevitCommand.mainView.Hide();
            CuttingSolid cuttingSolid = parameter as CuttingSolid;
            cuttingSolid.BoundLines = RevitModel.GetBoundLines();
            cuttingSolid.BoundLinesIds = RevitModel.ElementIdToString(cuttingSolid.BoundLines);
            RevitCommand.mainView.ShowDialog();
        }

        private bool CanSelectBoundLinesCommandExecute(object paramter)
        {
            return true;
        }
        #endregion

        #endregion


        #region Конструктор класса MainWindowViewModel
        public MainWindowViewModel(RevitModelForfard revitModel)
        {
            RevitModel = revitModel;

            CuttingSolidsCollection = new ObservableCollection<CuttingSolid>();

            #region Команды

            SelectTopLinesCommand = new LambdaCommand(OnSelectTopLinesCommandExecuted, CanSelectTopLinesCommandExecute);

            SelectBoundLinesCommand = new LambdaCommand(OnSelectBoundLinesCommandExecuted, CanSelectBoundLinesCommandExecute);

            AddCutSolidCommand = new LambdaCommand(OnAddCutSolidCommandExecuted, CanAddCutSolidCommandExecute);

            RemoveCutSolidCommand = new LambdaCommand(OnRemoveCutSolidCommandExecuted, CanRemoveCutSolidCommandExecute);

            #endregion
        }

        public MainWindowViewModel() { }
        #endregion
    }
}
