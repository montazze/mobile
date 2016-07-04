﻿using System;
using Acr.UserDialogs;
using Bit.App.Abstractions;
using Bit.App.Controls;
using Bit.App.Resources;
using Plugin.Connectivity.Abstractions;
using Xamarin.Forms;
using XLabs.Ioc;

namespace Bit.App.Pages
{
    public class SettingsEditFolderPage : ExtendedContentPage
    {
        private readonly string _folderId;
        private readonly IFolderService _folderService;
        private readonly IUserDialogs _userDialogs;
        private readonly IConnectivity _connectivity;

        public SettingsEditFolderPage(string folderId)
        {
            _folderId = folderId;
            _folderService = Resolver.Resolve<IFolderService>();
            _userDialogs = Resolver.Resolve<IUserDialogs>();
            _connectivity = Resolver.Resolve<IConnectivity>();

            Init();
        }

        private void Init()
        {
            var folder = _folderService.GetByIdAsync(_folderId).GetAwaiter().GetResult();
            if(folder == null)
            {
                // TODO: handle error. navigate back? should never happen...
                return;
            }

            var nameCell = new FormEntryCell(AppResources.Name);
            nameCell.Entry.Text = folder.Name.Decrypt();

            var deleteCell = new ExtendedTextCell { Text = AppResources.Delete, TextColor = Color.Red };
            deleteCell.Tapped += DeleteCell_Tapped;

            var mainTable = new ExtendedTableView
            {
                Intent = TableIntent.Settings,
                EnableScrolling = false,
                EnableSelection = false,
                HasUnevenRows = true,
                VerticalOptions = LayoutOptions.Start,
                BackgroundColor = Color.Gray,
                Margin = new Thickness(0, -1),
                Root = new TableRoot
                {
                    new TableSection()
                    {
                        nameCell
                    }
                }
            };

            if(Device.OS == TargetPlatform.iOS)
            {
                mainTable.RowHeight = -1;
                mainTable.EstimatedRowHeight = 70;
            }

            var deleteTable = new ExtendedTableView
            {
                Intent = TableIntent.Settings,
                EnableScrolling = false,
                EnableSelection = true,
                VerticalOptions = LayoutOptions.End,
                BackgroundColor = Color.Yellow,
                Margin = new Thickness(0, -1),
                Root = new TableRoot
                {
                    new TableSection()
                    {
                        deleteCell
                    }
                }
            };

            var saveToolBarItem = new ToolbarItem(AppResources.Save, null, async () =>
            {
                if(!_connectivity.IsConnected)
                {
                    AlertNoConnection();
                    return;
                }

                if(string.IsNullOrWhiteSpace(nameCell.Entry.Text))
                {
                    await DisplayAlert(AppResources.AnErrorHasOccurred, string.Format(AppResources.ValidationFieldRequired, AppResources.Name), AppResources.Ok);
                    return;
                }

                folder.Name = nameCell.Entry.Text.Encrypt();

                var saveTask = _folderService.SaveAsync(folder);
                _userDialogs.ShowLoading("Saving...", MaskType.Black);
                await saveTask;

                _userDialogs.HideLoading();
                await Navigation.PopModalAsync();
                _userDialogs.SuccessToast(nameCell.Entry.Text, "Folder updated.");
            }, ToolbarItemOrder.Default, 0);

            Title = "Edit Folder";
            Content = new ScrollView { Content = new StackLayout { Children = { mainTable, deleteTable } } };
            ToolbarItems.Add(saveToolBarItem);
            if(Device.OS == TargetPlatform.iOS)
            {
                ToolbarItems.Add(new DismissModalToolBarItem(this, "Cancel"));
            }

            if(!_connectivity.IsConnected)
            {
                AlertNoConnection();
            }
        }

        private async void DeleteCell_Tapped(object sender, EventArgs e)
        {
            // TODO: Validate the delete operation. ex. Cannot delete a folder that has sites in it?

            if(!await _userDialogs.ConfirmAsync(AppResources.DoYouReallyWantToDelete, null, AppResources.Yes, AppResources.No))
            {
                return;
            }

            var deleteTask = _folderService.DeleteAsync(_folderId);
            _userDialogs.ShowLoading("Deleting...", MaskType.Black);
            await deleteTask;
            _userDialogs.HideLoading();

            if((await deleteTask).Succeeded)
            {
                await Navigation.PopModalAsync();
                _userDialogs.SuccessToast("Folder deleted.");
            }
        }

        private void AlertNoConnection()
        {
            DisplayAlert(AppResources.InternetConnectionRequiredTitle, AppResources.InternetConnectionRequiredMessage, AppResources.Ok);
        }
    }
}