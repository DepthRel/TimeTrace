﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using TimeTrace.Model.Events;
using TimeTrace.Model.DBContext;
using TimeTrace.View.MainView.PersonalMapsCreatePages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace TimeTrace.ViewModel.MainViewModel.MapEventsViewModel
{
	/// <summary>
	/// View model of project list
	/// </summary>
	public class ProjectViewModel : BaseViewModel
	{
		#region Properties

		/// <summary>
		/// Reseived area
		/// </summary>
		public Area CurrentArea { get; set; }

		/// <summary>
		/// Available colors for choosing
		/// </summary>
		private Dictionary<string, string> ColorsTable { get; set; }

		private ObservableCollection<Project> buttonCollection;
		/// <summary>
		/// Binded collection of buttons
		/// </summary>
		public ObservableCollection<Project> ButtonCollection
		{
			get => buttonCollection;
			set
			{
				buttonCollection = value;
				OnPropertyChanged();
			}
		}

		private ObservableCollection<string> projectSuggestList;
		/// <summary>
		/// Filter tips
		/// </summary>
		public ObservableCollection<string> ProjectSuggestList
		{
			get => projectSuggestList;
			set
			{
				projectSuggestList = value;
				OnPropertyChanged();
			}
		}

		#endregion

		public SolidColorBrush GetCurrentColor { get => GetColorFromString(CurrentArea.Color.ToString()); }

		/// <summary>
		/// Standart constructor
		/// </summary>
		public ProjectViewModel(Area area)
		{
			CurrentArea = area;

			ColorsTable = new Dictionary<string, string>()
			{
				{ "Красный", "#d50000" },
				{ "Розовый", "#E67C73" },
				{ "Оранжевый", "#F4511E"},
				{ "Жёлтый", "#F6BF26" },
				{ "Светло-зелёный", "#33B679" },
				{ "Зелёный", "#0B8043" },
				{ "Голубой", "#039BE5" },
				{ "Синий", "#3F51B5" },
				{ "Светло-фиолетовый", "#7986CB" },
				{ "Фиолетовый", "#8E24AA" },
				{ "Чёрный", "#616161" }
			};

			ButtonCollection = new ObservableCollection<Project>();
			ProjectSuggestList = new ObservableCollection<string>();

			ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

			using (MainDatabaseContext db = new MainDatabaseContext())
			{
				foreach (var i in db.Projects
					.Where(i => i.AreaId == CurrentArea.Id && !i.IsDelete && i.EmailOfOwner == (string)localSettings.Values["email"])
					.Select(i => i))
				{
					ButtonCollection.Add(i);
					//ButtonCollection.Add(NewProjectButtonCreate(i));
				}
			}
		}

		/// <summary>
		/// Creation of new category
		/// </summary>
		public async void ProjectCreate()
		{
			Project newProject = await ProjectCreationDialogAsync(null);

			if (newProject != null)
			{
				// Adding area into DB
				using (var db = new MainDatabaseContext())
				{
					db.Projects.Add(newProject);
					db.SaveChanges();
				}

				ButtonCollection.Add(newProject);
				//ButtonCollection.Add(NewProjectButtonCreate(newProject));
			}
		}

		/// <summary>
		/// Navigate to map event create page
		/// </summary>
		/// <param name="sender">Object sender</param>
		/// <param name="e">Parameters</param>
		public void ProjectSelect(object sender, RoutedEventArgs e)
		{
			using (MainDatabaseContext db = new MainDatabaseContext())
			{
				(Application.Current as App)?.AppFrame.Navigate(typeof(PersonalEventCreatePage), db.Projects.FirstOrDefault(i => i.Id == (string)(sender as Button).Tag));
			}
		}

		public void ProjectsBulkRemoval()
		{

		}

		/// <summary>
		/// Removing of selected project
		/// </summary>
		public async void ProjectRemoveAsync(object sender, RoutedEventArgs e)
		{
			using (MainDatabaseContext db = new MainDatabaseContext())
			{
				// Get selected Area
				var selectedProject = db.Projects.First(i => i.Id == (string)(sender as MenuFlyoutItem).Tag);

				ContentDialog confirmDialog = new ContentDialog()
				{
					Title = "Подтверждение удаления",
					Content = $"Вы уверены что хотите удалить проект {selectedProject.Name}?\n" +
							  $"Удаление приведен к потере всех событий внутри проекта!",
					PrimaryButtonText = "Удалить",
					CloseButtonText = "Отмена",
					DefaultButton = ContentDialogButton.Close,
				};

				var result = await confirmDialog.ShowAsync();

				if (result == ContentDialogResult.Primary)
				{
					// Remove Area from UI panel
					//ButtonCollection.Remove(ButtonCollection.First(i => (string)(i as Button).Tag == selectedProject.Id));

					// Remove all events in this project
					foreach (var mapEvent in db.MapEvents.Where(i => i.Id == selectedProject.Id))
					{
						db.MapEvents.FirstOrDefault(i => i.Id == mapEvent.Id).IsDelete = true;
					}

					// Remove Project from UI
					db.Projects.FirstOrDefault(i => i.Id == selectedProject.Id).IsDelete = true;

					db.SaveChanges();

					await new MessageDialog($"Проект {selectedProject.Name} со всеми событиями внутри был удалён",
						"Операция завершена успешно").ShowAsync();
				}
			}
		}

		/// <summary>
		/// Edit of selected project
		/// </summary>
		public async void ProjectEditAsync(object sender, RoutedEventArgs e)
		{
			using (MainDatabaseContext db = new MainDatabaseContext())
			{
				// Get selected Project
				var selectedProject = db.Projects.First(i => i.Id == (string)(sender as MenuFlyoutItem).Tag);

				// Get edited Project object
				var editedProject = await ProjectCreationDialogAsync(selectedProject);

				if (editedProject == null)
				{
					return;
				}

				// If project was edit
				if (editedProject.Name != selectedProject.Name || editedProject.Description != selectedProject.Description || editedProject.Color != selectedProject.Color)
				{
					selectedProject.Name = editedProject.Name;
					selectedProject.Description = editedProject.Description;
					selectedProject.Color = editedProject.Color;

					selectedProject.UpdateAt = DateTime.UtcNow;

					// Update project in database
					db.Projects.Update(selectedProject);
					db.SaveChanges();

					// Update current button
					/*var index = ButtonCollection.IndexOf(ButtonCollection.First(i => (string)i.Tag == selectedProject.Id));
					ButtonCollection.RemoveAt(index);
					ButtonCollection.Insert(index, NewProjectButtonCreate(selectedProject));*/
				}
			}
		}

		/// <summary>
		/// Filtration of input filters
		/// </summary>
		/// <param name="sender">Input filter</param>
		/// <param name="args">Event args</param>
		public void ProjectFilter(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (ButtonCollection == null) return;

			if (args.CheckCurrent())
			{
				ProjectSuggestList.Clear();
			}

			// Remove all buttons for adding relevant filter
			ButtonCollection.Clear();

			// Select all calendars
			if (string.IsNullOrEmpty(sender.Text))
			{
				ProjectSuggestList.Clear();

				using (MainDatabaseContext db = new MainDatabaseContext())
				{
					foreach (var i in db.Projects.Where(i => i.AreaId == CurrentArea.Id).Select(i => i))
					{
						//ButtonCollection.Add(NewProjectButtonCreate(i));
					}
				}
			}

			else
			{
				using (MainDatabaseContext db = new MainDatabaseContext())
				{
					foreach (var i in db.Projects.
										Where(i => i.Name.ToLowerInvariant().Contains(sender.Text.ToLowerInvariant()) && i.AreaId == CurrentArea.Id).
										Select(i => i))
					{
						if (!ProjectSuggestList.Contains(i.Name))
						{
							ProjectSuggestList.Add(i.Name);
						}

						//ButtonCollection.Add(NewProjectButtonCreate(i));
					}
				}
			}
		}

		/// <summary>
		/// Click on Find button
		/// </summary>
		/// <param name="sender">Object</param>
		/// <param name="args">Args</param>
		public void ProjectFilterQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		{
			ProjectSuggestList.Clear();

			if (ButtonCollection != null)
			{
				using (MainDatabaseContext db = new MainDatabaseContext())
				{
					var term = args.QueryText.ToLower();
					foreach (var i in db.Projects.Where(i => i.Name.ToLower().Contains(term) && i.AreaId == CurrentArea.Id).Select(i => i))
					{
						//ButtonCollection.Add(NewProjectButtonCreate(i));
					}
				}
			}
		}

		/// <summary>
		/// Creation of edition project dialog
		/// </summary>
		/// <param name="project"><see cref="Project"/> object</param>
		/// <returns>Edited object of project or new object if created</returns>
		private async Task<Project> ProjectCreationDialogAsync(Project project)
		{
			#region Text boxes

			TextBox name = new TextBox()
			{
				Header = "Название",
				PlaceholderText = "Название нового проекта",
				Text = project?.Name ?? string.Empty,
				Margin = new Thickness(0, 0, 0, 10),
				MaxLength = 30,
				SelectionStart = project?.Name?.Length ?? 0,
			};

			TextBox description = new TextBox()
			{
				Header = "Описание",
				PlaceholderText = "Краткое описание проекта",
				Text = project?.Description ?? string.Empty,
				Margin = new Thickness(0, 0, 0, 10),
				MaxLength = 50,
			};

			#endregion

			#region Color box

			ComboBox colors = new ComboBox()
			{
				Header = "Цвет проекта",
				Width = 300,
			};

			// Building list of colors for choosing
			for (int i = 0; i < ColorsTable.Count; i++)
			{
				var brush = GetColorFromString(ColorsTable.ElementAt(i).Value);

				// Ring of color
				Ellipse colorRing = new Ellipse
				{
					Fill = brush,
					Width = 16,
					Height = 16,
				};

				// Center ring with white color
				Ellipse innerColorRing = new Ellipse
				{
					Fill = new SolidColorBrush(new Color() { A = 255, R = 255, G = 255, B = 255 }),
					Width = 12,
					Height = 12,
				};

				// Item panel with color & name of color
				StackPanel colorBoxItem = new StackPanel();
				colorBoxItem.Orientation = Orientation.Horizontal;

				Grid colorGrid = new Grid();
				colorGrid.Children.Add(colorRing);
				colorGrid.Children.Add(innerColorRing);

				colorBoxItem.Children.Add(colorGrid);
				colorBoxItem.Children.Add(new TextBlock()
				{
					Text = $"{ColorsTable.ElementAt(i).Key}",
					Margin = new Thickness(8, 0, 0, 0),
					VerticalAlignment = VerticalAlignment.Center
				});

				colors.Items.Add(colorBoxItem);
			}

			#endregion

			// Start selected color or default red
			colors.SelectedIndex = (project == null) ? 0 : project.Color - 1;

			StackPanel panel = new StackPanel();
			panel.Children.Add(name);
			panel.Children.Add(description);
			panel.Children.Add(colors);

			ContentDialog dialog = new ContentDialog()
			{
				Title = (project == null) ? "Создание нового проекта" : "Редактирование проекта",
				Content = panel,
				PrimaryButtonText = (project == null) ? "Создать" : "Изменить",
				CloseButtonText = "Отложить",
				DefaultButton = ContentDialogButton.Primary,
			};

			var result = await dialog.ShowAsync();

			if (result == ContentDialogResult.Primary)
			{
				if (project == null && string.IsNullOrEmpty(name.Text))
				{
					await new MessageDialog("Не заполнено имя для нового проекта", "Ошибка добавления нового проекта").ShowAsync();

					return null;
				}
				else
				{
					if (string.IsNullOrEmpty(name.Text))
					{
						await new MessageDialog("Не заполнено имя изменяемого проекта", "Ошибка изменения проекта").ShowAsync();

						return null;
					}
				}

				// New area object for adding into Database
				return new Project()
				{
					Name = name.Text,
					Description = description.Text,
					Color = colors.SelectedIndex + 1,
					AreaId = CurrentArea.Id,
				};
			}
			else
			{
				return project;
			}
		}

		/// <summary>
		/// Factory of buttons
		/// </summary>
		/// <param name="project"></param>
		/// <returns>New button object</returns>
		private Button NewProjectButtonCreate(Project project)
		{
			// Edit menu item
			MenuFlyoutItem editItem = new MenuFlyoutItem()
			{
				Text = "Редактировать",
				Icon = new SymbolIcon(Symbol.Edit),
				Tag = project.Id,
			};
			editItem.Click += ProjectEditAsync;

			// Remove menu item
			MenuFlyoutItem removeItem = new MenuFlyoutItem()
			{
				Text = "Удалить",
				Icon = new SymbolIcon(Symbol.Delete),
				Tag = project.Id,
			};
			removeItem.Click += ProjectRemoveAsync;

			// Main menu for button
			MenuFlyout menuForButton = new MenuFlyout();
			menuForButton.Items.Add(editItem);
			menuForButton.Items.Add(removeItem);

			// Creating area button for adding in page
			Button newButton = new Button()
			{
				BorderBrush = GetColorFromString(ColorsTable.ElementAt(project.Color - 1).Value),
				Content = project,
				Tag = project.Id,
				ContextFlyout = menuForButton
			};
			newButton.Click += ProjectSelect;

			return newButton;
		}

		/// <summary>
		/// Get the color brush from string
		/// </summary>
		/// <param name="color">Input string color</param>
		/// <returns>New color <see cref="Brush"/></returns>
		private SolidColorBrush GetColorFromString(string color)
		{
			var colorString = color.Replace("#", string.Empty);

			var r = byte.Parse(colorString.Substring(0, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
			var g = byte.Parse(colorString.Substring(2, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
			var b = byte.Parse(colorString.Substring(4, 2), System.Globalization.NumberStyles.AllowHexSpecifier);

			Color localColor = Color.FromArgb(255, r, g, b);
			return new SolidColorBrush(localColor);
		}
	}
}