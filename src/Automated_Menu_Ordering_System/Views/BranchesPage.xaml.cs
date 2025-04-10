﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Automated_Menu_Ordering_System.Contracts.Services;
using Microsoft.UI.Xaml.Controls;
using Syncfusion.UI.Xaml.DataGrid;
using WinUIEx;
using Microsoft.UI.Xaml;
using Npgsql;

namespace Automated_Menu_Ordering_System.Views;

public class Branch
{
    public int Id
    {
        get; set;
    }
    public string Name
    {
        get; set;
    }
}

public sealed partial class BranchesPage : Page
{

    public ObservableCollection<Branch> Branches
    {
        get; private set;
    }

    private bool CurrentlyAddingNewItem
    {
        get; set;
    }

    private Branch? BranchBeingAdded
    {
        get; set;
    }

    private bool IsEditingNew
    {
        get; set;
    }

    public BranchesPage()
    {
        this.InitializeComponent();
        Branches = new ObservableCollection<Branch>();
        LoadData();
    }

    public async void LoadData()
    {
        var reader = App.GetService<DatabaseService>().get_branches();
        if (!reader.HasRows)
        {
            reader.Close();
            return;
        }
        Branches.Clear();
        while (reader.Read())
        {
            var branch = new Branch
            {
                Id = Convert.ToInt32(reader["id"]),
                Name = reader["name"].ToString()
            };
            Branches.Add(branch);
        }
        reader.Close();
        sfDataGrid.ItemsSource = Branches;
    }

    private void StartEdit(Branch branch)
    {
        BranchBeingAdded = branch;
        CurrentlyAddingNewItem = true;
        sfDataGrid.SelectedItem = branch;
        sfDataGrid.View.MoveCurrentTo(branch);
        sfDataGrid.AllowDeleting = false;
        sfDataGrid.AllowEditing = true;
        sfDataGrid.Columns[0].IsReadOnly = true;

        HiddenButtons.Visibility = Visibility.Visible;
        ButtonsPanel.Visibility = Visibility.Collapsed;
    }

    private void EndEdit()
    {
        BranchBeingAdded = null;
        CurrentlyAddingNewItem = false;
        sfDataGrid.AllowEditing = false;
        sfDataGrid.AllowDeleting = true;

        HiddenButtons.Visibility = Visibility.Collapsed;
        ButtonsPanel.Visibility = Visibility.Visible;
    }

    private void Delete(Branch branch)
    {
        App.GetService<DatabaseService>().delete_branch(branch.Id);
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var newBranch = new Branch
        {
            Id = 0,
            Name = ""
        };
        Branches.Add(newBranch);
        IsEditingNew = true;
        StartEdit(newBranch);
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (sfDataGrid.SelectedItem is Branch branch)
        {
            IsEditingNew = false;
            StartEdit(branch);
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var errorDialog = new ContentDialog
        {
            Title = "Error",
            CloseButtonText = "OK",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };
        try
        {
            foreach (var item in sfDataGrid.SelectedItems)
            {
                if (item is Branch branch)
                {
                    Delete(branch);
                    Branches.Remove(branch);
                }
            }
        }
        catch (Exception ex)
        {
            errorDialog.Content = ex.Message;
            await errorDialog.ShowAsync();
        }
    }

    private async void DoneButton_Click(object sender, RoutedEventArgs e)
    {
        if (!CurrentlyAddingNewItem) return;
        var errorDialog = new ContentDialog
        {
            Title = "Error",
            CloseButtonText = "OK",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };


        if (sfDataGrid.SelectedItem is Branch branch)
        {
            try
            {
                if (IsEditingNew)
                    App.GetService<DatabaseService>().insert_branch(branch.Name);
                else
                    App.GetService<DatabaseService>().update_branch(branch.Id, branch.Name);
            }
            catch (Exception ex)
            {
                errorDialog.Content = ex.Message;
                await errorDialog.ShowAsync();
                Branches.Remove(branch);
            }
        }
        EndEdit();
        Branches.Clear();
        LoadData();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentlyAddingNewItem)
        {
            Branches.Remove(BranchBeingAdded);
        }
        EndEdit();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        Branches.Clear();
        LoadData();
    }

    private void sfDataGrid_SelectionChanged(object sender, Syncfusion.UI.Xaml.Grids.GridSelectionChangedEventArgs e)
    {
        if (CurrentlyAddingNewItem)
        {
            sfDataGrid.SelectedItem = BranchBeingAdded;
        }
    }

    private async void sfDataGrid_RecordDeleting(object sender, RecordDeletingEventArgs e)
    {
        var errorDialog = new ContentDialog
        {
            Title = "Error",
            CloseButtonText = "OK",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };
        try
        {
            foreach (var item in sfDataGrid.SelectedItems)
            {
                if (item is Branch branch)
                {
                    Delete(branch);
                }
            }
        }
        catch (Exception ex)
        {
            errorDialog.Content = ex.Message;
            await errorDialog.ShowAsync();
        }
    }
}
