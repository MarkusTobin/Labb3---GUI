﻿using Labb3___GUI.Model;
using Labb3___GUI.ViewModel;
using System.Windows;


namespace Labb3___GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}