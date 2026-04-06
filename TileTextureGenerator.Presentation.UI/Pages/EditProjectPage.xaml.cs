using TileTextureGenerator.Presentation.UI.ViewModels;

namespace TileTextureGenerator.Presentation.UI.Pages;

/// <summary>
/// Page for editing a project.
/// Receives EditProjectViewModel from navigation parameters.
/// </summary>
[QueryProperty(nameof(ViewModel), "ViewModel")]
public partial class EditProjectPage : ContentPage
{
    public EditProjectPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// ViewModel passed via navigation.
    /// Setting this property assigns it to BindingContext and updates the title.
    /// </summary>
    public EditProjectViewModel? ViewModel
    {
        set
        {
            if (value != null)
            {
                // Set Title BEFORE BindingContext to avoid binding issues
                Title = value.ShellTitle;
                BindingContext = value;
                OnPropertyChanged(nameof(Title));
            }
        }
    }

    /// <summary>
    /// Navigates back to ManageProjectList when Close is clicked.
    /// </summary>
    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ManageProjectListPage");
    }

    /// <summary>
    /// Called when user presses hardware back button.
    /// Navigates back to ManageProjectList.
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        // Navigate to ManageProjectList instead of default back behavior
        Task.Run(async () => await Shell.Current.GoToAsync("//ManageProjectListPage"));
        return true; // Prevent default back navigation
    }
}
