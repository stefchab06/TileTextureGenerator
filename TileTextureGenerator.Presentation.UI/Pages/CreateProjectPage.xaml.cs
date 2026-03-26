using TileTextureGenerator.Presentation.UI.ViewModels;

namespace TileTextureGenerator.Presentation.UI.Pages;

/// <summary>
/// Page for creating a new project.
/// Simple vertical layout with project name, type selection, and create button.
/// </summary>
public partial class CreateProjectPage : ContentPage
{
    public CreateProjectPage(CreateProjectViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// Handles Picker selection change to update ViewModel.
    /// </summary>
    private void OnProjectTypeSelected(object? sender, EventArgs e)
    {
        if (sender is Picker picker && BindingContext is CreateProjectViewModel vm)
        {
            if (picker.SelectedItem is ProjectTypeItem selectedItem)
            {
                vm.SelectedProjectType = selectedItem.TechnicalName;
            }
        }
    }
}
