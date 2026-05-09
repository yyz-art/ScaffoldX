using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScaffoldX.Plugin.Scaffold.ViewModels;

namespace ScaffoldX.Plugin.Scaffold.Views;

public partial class Step1View : UserControl
{
    public Step1View()
    {
        InitializeComponent();
    }

    private void OnCollectionCardClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is Step1ViewModel vm)
        {
            vm.SelectedProjectType = "Collection";
        }
    }

    private void OnVisionCardClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is Step1ViewModel vm)
        {
            vm.SelectedProjectType = "Vision";
        }
    }

    private void OnSystemCardClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is Step1ViewModel vm)
        {
            vm.SelectedProjectType = "System";
        }
    }
}
