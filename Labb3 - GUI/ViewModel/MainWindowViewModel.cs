﻿using Labb3___GUI.Command;
using Labb3___GUI.Dialogs;
using Labb3___GUI.Model;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using Labb3___GUI.MongoDB;
using MongoDB.Driver;
using System.Linq;
using MongoDB.Driver.Linq;
using System.ComponentModel;


namespace Labb3___GUI.ViewModel
{
    internal class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<QuestionPackViewModel> Packs { get; set; } = new ObservableCollection<QuestionPackViewModel>();
        public ConfigurationViewModel ConfigurationViewModel { get; }
        public PlayerViewModel PlayerViewModel { get; }
        public CategoryViewModel CategoryViewModel { get; }
        private QuestionPackViewModel? _activePack;

        public DelegateCommand StartQuizCommand { get; }
        public DelegateCommand SetConfigModeCommand { get; }
        public DelegateCommand SetPlayModeCommand { get; }
        public DelegateCommand AddPackCommand { get; }
        public DelegateCommand RemovePackCommand { get; }
        public DelegateCommand EditPackCommand { get; }
        public DelegateCommand SelectPackCommand { get; }
        public DelegateCommand CloseDialogCommand { get; }
        public DelegateCommand ToggleFullScreenCommand { get; set; }
        public DelegateCommand ExitAndSaveCommand { get; }
        public DelegateCommand OpenEditCategoriesCommand { get; }

        public MainWindowViewModel()
        {
            ConfigurationViewModel = new ConfigurationViewModel(this);
            Task.Run(async () => await LoadFromMongoDB()).Wait();
            CategoryViewModel = new CategoryViewModel();

            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("MarkusTobin");
            var mongoDBService = new MongoDBService(database);

            if (Packs == null || Packs.Count == 0)
            {
                var defaultPack = new QuestionPack("My Question Pack", Difficulty.Medium, 30, null, "No Category Set");
                var defaultQuestion = new Question("What is 2 + 2?", "4", "3", "5", "6");
                var defaultQuestion2 = new Question("What is 4 + 4?", "8", "3", "5", "6");
                defaultPack.Questions.Add(defaultQuestion);
                defaultPack.Questions.Add(defaultQuestion2);

                var defaultPackViewModel = new QuestionPackViewModel(defaultPack);
                Packs = new ObservableCollection<QuestionPackViewModel> { defaultPackViewModel };
                ActivePack = defaultPackViewModel;
            }

            PlayerViewModel = new PlayerViewModel(this, mongoDBService);
            if (ActivePack?.Questions != null && ActivePack.Questions.Any() && IsPlayMode)
            {
                PlayerViewModel.StartNewQuiz(ActivePack.Questions.ToList());
            }
            else
            {
                Debug.WriteLine("Error: ActivePack does not contain any questions.");
            }

            AddPackCommand = new DelegateCommand(AddPack);
            RemovePackCommand = new DelegateCommand(RemovePack, CanRemovePack);
            EditPackCommand = new DelegateCommand(EditPack, CanEditPack);
            SelectPackCommand = new DelegateCommand(SelectPack);
            CloseDialogCommand = new DelegateCommand(CloseDialogWindow);
            StartQuizCommand = new DelegateCommand(StartQuiz);
            SetConfigModeCommand = new DelegateCommand(_ => SetConfigMode());
            SetPlayModeCommand = new DelegateCommand(_ => SetPlayMode());
            ToggleFullScreenCommand = new DelegateCommand(ToggleFullScreen);
            ExitAndSaveCommand = new DelegateCommand(ExitAndSave);
            OpenEditCategoriesCommand = new DelegateCommand(_ => OpenEditCategories());

            IsConfigMode = true;
            IsPlayMode = false;
        }

        private void OpenEditCategories()
        {
            var editCategoriesDialog = new EditCategoriesDialog
            {
                DataContext = new EditCategoriesViewModel(CategoryViewModel.Categories)
            };
            editCategoriesDialog.ShowDialog();
        }

        private async Task SaveToMongoDB(List<QuestionPack> questionPacks, List<string> categories)
        {
            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("MarkusTobin");
            var mongoDBService = new MongoDBService(database);
            await mongoDBService.SaveToMongoDBService(questionPacks, categories);
        }

        private async Task LoadFromMongoDB()
        {
            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("MarkusTobin");
            var mongoDBService = new MongoDBService(database);
            var (questionPacks, categories) = await mongoDBService.LoadFromMongoDBService();

            foreach (var pack in questionPacks)
            {
                Packs.Add(new QuestionPackViewModel(pack));
            }
            if (Packs.Any())
            {
                ActivePack = Packs.First();
            }
        }

        private void SelectPack(object selectedPackObj)
        {
            if (selectedPackObj is QuestionPackViewModel selectedPack)
            {
                ActivePack = selectedPack;
            }
        }

        public void SetActivePack(QuestionPack newPack)
        {
            ActivePack = new QuestionPackViewModel(newPack);
            RaisePropertyChanged(nameof(ActivePack));
            PlayerViewModel.StartNewQuiz(ActivePack.Questions.ToList());
        }

        public void CloseDialogWindow(object parameter)
        {
            if (parameter is Window window)
            {
                window.DialogResult = false;
                window.Close();
            }
        }

        private void AddPack(object parameter)
        {
            var newPack = new QuestionPack("Default name", Difficulty.Medium, 30);
            var newPackViewModel = new QuestionPackViewModel(newPack);

            newPackViewModel.Categories = CategoryViewModel.Categories;
            newPackViewModel.SelectedCategory = CategoryViewModel.SelectedCategory;

            var createNewPackDialog = new CreateNewPackDialog()
            {
                DataContext = newPackViewModel
            };

            newPackViewModel.OpenEditCategoriesCommand = OpenEditCategoriesCommand;
            newPackViewModel.Categories = CategoryViewModel.Categories;

            bool? result = createNewPackDialog.ShowDialog();
            if (result == true)
            {
                Packs.Add(newPackViewModel);
                ActivePack = newPackViewModel;
                RaisePropertyChanged(nameof(Packs));
            }
        }
        private void RemovePack(object parameter)
        {
            if (ActivePack != null)
            {
                Packs.Remove(ActivePack);
                ActivePack = Packs.FirstOrDefault();
                RaisePropertyChanged(nameof(ActivePack));
            }
        }
        private bool CanRemovePack(object parameter)
        {
            return ActivePack != null;
        }
        public void EditPack(object parameter)
        {
            if (ActivePack != null)
            {
                ActivePack.Categories = CategoryViewModel.Categories;
                ActivePack.OpenEditCategoriesCommand = OpenEditCategoriesCommand;

                if (ActivePack.SelectedCategory == null || !CategoryViewModel.Categories.Contains(ActivePack.SelectedCategory))
                {
                    ActivePack.SelectedCategory = CategoryViewModel.Categories.FirstOrDefault() ?? string.Empty;
                }

                var dialog = new PackOptionsDialog
                {
                    DataContext = ActivePack
                };
                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    ActivePack.QuestionPack.Category = ActivePack.SelectedCategory;
                    RaisePropertyChanged(nameof(ActivePack));
                }
            }
        }
        private bool CanEditPack(object parameter)
        {
            return ActivePack != null;
        }
        public QuestionPackViewModel? ActivePack
        {
            get => _activePack;
            set
            {
                _activePack = value;
                RaisePropertyChanged();
                ConfigurationViewModel.RaisePropertyChanged(nameof(ConfigurationViewModel.ActivePack));
                ConfigurationViewModel.ActiveQuestion = ActivePack?.Questions.FirstOrDefault();
            }
        }
        private bool _isPlayMode;
        public bool IsPlayMode
        {
            get => _isPlayMode;
            set
            {
                _isPlayMode = value;
                RaisePropertyChanged();
                if (_isPlayMode)
                {
                    StartGame();
                }
            }
        }
        private void StartGame()
        {
            PlayerViewModel.StartNewQuiz(ActivePack.Questions.ToList());
        }
        private bool _isConfigMode;
        public bool IsConfigMode
        {
            get => _isConfigMode;
            set
            {
                _isConfigMode = value;
                RaisePropertyChanged();
            }
        }
        private void StartQuiz(object parameter)
        {
            if (ActivePack == null || !ActivePack.Questions.Any())
            {
                return;
            }
            IsPlayMode = true;
            IsConfigMode = false;
            PlayerViewModel.StartNewQuiz(ActivePack.Questions.ToList());
        }
        private void SetConfigMode()
        {
            IsConfigMode = true;
            IsPlayMode = false;
        }
        private void SetPlayMode()
        {
            IsPlayMode = true;
            IsConfigMode = false;
        }
        private void ToggleFullScreen(object parameter)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow;

            if (mainWindow.WindowState == WindowState.Normal)
            {
                mainWindow.WindowState = WindowState.Maximized;
                mainWindow.WindowStyle = WindowStyle.None;
            }
            else
            {
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.WindowStyle = WindowStyle.SingleBorderWindow;
            }
        }
        private async void ExitAndSave(object? parameter)
        {
            var result = System.Windows.MessageBox.Show(
                "You will save all Questionpacks and Categories on exit. Do you want to continue?",
                "Confirm Exit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                var questionPacks = Packs.Select(p => p.QuestionPack).ToList();
                var categories = CategoryViewModel.Categories.ToList();
                await SaveToMongoDB(questionPacks, categories);
                System.Windows.Application.Current.Shutdown();
            }
        }
    }
}