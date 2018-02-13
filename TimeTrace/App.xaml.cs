﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TimeTrace.View;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace TimeTrace
{
	/// <summary>
	/// Обеспечивает зависящее от конкретного приложения поведение, дополняющее класс Application по умолчанию.
	/// </summary>
	sealed partial class App : Application
	{
		/// <summary>
		/// Инициализирует одноэлементный объект приложения.  Это первая выполняемая строка разрабатываемого
		/// кода; поэтому она является логическим эквивалентом main() или WinMain().
		/// </summary>
		public App()
		{
			//AppSignInWithToken().GetAwaiter();

			this.InitializeComponent();
			this.Suspending += OnSuspending;
		}

		/// <summary>
		/// Вызывается при обычном запуске приложения пользователем.  Будут использоваться другие точки входа,
		/// например, если приложение запускается для открытия конкретного файла.
		/// </summary>
		/// <param name="e">Сведения о запросе и обработке запуска.</param>
		protected override async void OnLaunched(LaunchActivatedEventArgs e)
		{
			// Load data base
			/*if (await ApplicationData.Current.LocalFolder.TryGetItemAsync(@"DataBase\MapEvents.db") == null)
			{
				StorageFile databaseFile = await Package.Current.InstalledLocation.GetFileAsync(@"DataBase\MapEvents.db");
				await databaseFile.CopyAsync(ApplicationData.Current.LocalFolder);
			}*/

			Frame rootFrame = Window.Current.Content as Frame;

			// Не повторяйте инициализацию приложения, если в окне уже имеется содержимое,
			// только обеспечьте активность окна
			if (rootFrame == null)
			{
				// Создание фрейма, который станет контекстом навигации, и переход к первой странице
				rootFrame = new Frame();

				rootFrame.NavigationFailed += OnNavigationFailed;

				if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
				{
					//TODO: Загрузить состояние из ранее приостановленного приложения
				}

				// Размещение фрейма в текущем окне
				Window.Current.Content = rootFrame;
			}

			if (e.PrelaunchActivated == false)
			{
				if (rootFrame.Content == null)
				{
					// Если стек навигации не восстанавливается для перехода к первой странице,
					// настройка новой страницы путем передачи необходимой информации в качестве параметра
					// параметр
					rootFrame.Navigate(typeof(View.MainView.StartPage), e.Arguments);
					//rootFrame.Navigate(typeof(View.AuthenticationView.SignInPage), e.Arguments);
				}
				// Обеспечение активности текущего окна
				Window.Current.Activate();

				// Скрытие TitleBar
				ExtendAcrylicIntoTitleBar();

				// Активация навигации
				SystemNavigationManager.GetForCurrentView().BackRequested += ((sender, args) =>
				{
					Frame frame = Window.Current.Content as Frame;

					if (frame.CanGoBack)
					{
						frame.GoBack();
						args.Handled = true;
					}
				});

				// Лямбда для навигации
				rootFrame.Navigated += (s, args) =>
				{
					if (rootFrame.CanGoBack) // если можно перейти назад, показываем кнопку
					{
						SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
												AppViewBackButtonVisibility.Visible;
					}
					else
					{
						SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
							AppViewBackButtonVisibility.Collapsed;
					}

				};
			}
		}

		/// <summary>
		/// Скрытие TitleBar
		/// </summary>
		private void ExtendAcrylicIntoTitleBar()
		{
			CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
			ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
			titleBar.ButtonBackgroundColor = Colors.Transparent;
			titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
		}

		/// <summary>
		/// Вызывается в случае сбоя навигации на определенную страницу
		/// </summary>
		/// <param name="sender">Фрейм, для которого произошел сбой навигации</param>
		/// <param name="e">Сведения о сбое навигации</param>
		void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
		{
			throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
		}

		/// <summary>
		/// Вызывается при приостановке выполнения приложения.  Состояние приложения сохраняется
		/// без учета информации о том, будет ли оно завершено или возобновлено с неизменным
		/// содержимым памяти.
		/// </summary>
		/// <param name="sender">Источник запроса приостановки.</param>
		/// <param name="e">Сведения о запросе приостановки.</param>
		private void OnSuspending(object sender, SuspendingEventArgs e)
		{
			var deferral = e.SuspendingOperation.GetDeferral();
			//TODO: Сохранить состояние приложения и остановить все фоновые операции
			deferral.Complete();
		}

		/// <summary>
		/// Попытка входа в систему с помощью токена
		/// </summary>
		private async Task AppSignInWithToken()
		{
			var res = await Model.UserFileWorker.LoadUserEmailAndTokenFromFileAsync();

			if (string.IsNullOrEmpty(res.email) || string.IsNullOrEmpty(res.token))
			{
				return;
			}

			try
			{
				var requestResult = await Model.UserRequests.PostRequestAsync(Model.UserRequests.PostRequestDestination.SignInWithToken, null);

				if (requestResult == 0)
				{
					if (Window.Current.Content is Frame frame)
					{
						frame.Navigate(typeof(View.MainView.StartPage));
					}
				}
			}
			catch (Exception ex)
			{
				await (new Windows.UI.Popups.MessageDialog($"{ex.Message}\n" +
					$"Ошибка входа, удаленный сервер не доступен. Повторите попытку позже", "Ошибка входа")).ShowAsync();
			}
		}
	}
}
