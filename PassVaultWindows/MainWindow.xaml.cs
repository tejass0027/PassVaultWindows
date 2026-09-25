using System;
using System.Collections.Generic;
using System.Windows;
using PassVaultWindows.Data;
using PassVaultWindows.Views;

namespace PassVaultWindows;

public partial class MainWindow : Window
{
    private enum AppRoute
    {
        Welcome, CreatePattern, SecurityQuestionsSetup, BiometricOptIn, Login, ForgotPattern,
        VaultList, EntryDetail, AddEditEntry, Settings, ChangePattern, ManageSecurityQuestions,
        BackupExport, BackupImport, LoginActivity, PhotoVault, HiddenVaultSetup,
        HiddenHub, HiddenPasswords, HiddenEntryDetail, HiddenAddEdit, HiddenPhotos, HiddenNotes
    }

    private static readonly HashSet<AppRoute> HiddenRoutes = new()
    {
        AppRoute.HiddenHub, AppRoute.HiddenPasswords, AppRoute.HiddenEntryDetail,
        AppRoute.HiddenAddEdit, AppRoute.HiddenPhotos, AppRoute.HiddenNotes
    };

    private static readonly HashSet<AppRoute> ProtectedRoutes = new()
    {
        AppRoute.VaultList, AppRoute.EntryDetail, AppRoute.AddEditEntry, AppRoute.Settings,
        AppRoute.ChangePattern, AppRoute.ManageSecurityQuestions, AppRoute.BackupExport,
        AppRoute.BackupImport, AppRoute.LoginActivity, AppRoute.PhotoVault, AppRoute.HiddenVaultSetup
    };

    private readonly AppState _appState;
    private AppRoute _currentRoute;
    private DateTime? _deactivatedAt;

    public MainWindow(AppState appState)
    {
        InitializeComponent();
        _appState = appState;

        Activated += OnActivated;
        Deactivated += OnDeactivated;
        _appState.IsUnlockedChanged += OnIsUnlockedChanged;
        _appState.IsHiddenVaultUnlockedChanged += OnIsHiddenVaultUnlockedChanged;

        if (_appState.IsOnboarded)
        {
            ShowLogin();
        }
        else
        {
            ShowWelcome();
        }
    }

    // --- Auto-lock: if the window lost focus longer than the configured timeout, lock it. ---

    private void OnDeactivated(object? sender, EventArgs e) => _deactivatedAt = DateTime.UtcNow;

    private void OnActivated(object? sender, EventArgs e)
    {
        if (_deactivatedAt == null || (!_appState.VaultRepository.IsUnlocked && !_appState.HiddenNotesRepository.IsUnlocked))
        {
            _deactivatedAt = null;
            return;
        }
        var elapsedSeconds = (DateTime.UtcNow - _deactivatedAt.Value).TotalSeconds;
        _deactivatedAt = null;
        if (elapsedSeconds >= _appState.AuthPrefs.AutoLockSeconds)
        {
            if (_appState.VaultRepository.IsUnlocked)
            {
                _appState.Lock();
            }
            if (_appState.HiddenNotesRepository.IsUnlocked)
            {
                _appState.LockHiddenVault();
            }
        }
    }

    // Single source of truth: whenever the vault becomes locked (auto-lock or the lock
    // button), send the user back to the login screen if they were on a protected screen.
    private void OnIsUnlockedChanged(bool isUnlocked)
    {
        if (!isUnlocked && _appState.IsOnboarded && ProtectedRoutes.Contains(_currentRoute))
        {
            ShowLogin();
        }
    }

    // Same, but for the hidden vault - entirely independent of the main vault's state.
    private void OnIsHiddenVaultUnlockedChanged(bool isUnlocked)
    {
        if (!isUnlocked && HiddenRoutes.Contains(_currentRoute))
        {
            ShowLogin();
        }
    }

    private void Navigate(AppRoute route, FrameworkElement view)
    {
        _currentRoute = route;
        MainContent.Content = view;
    }

    // --- Onboarding ---

    public void ShowWelcome() => Navigate(AppRoute.Welcome, new WelcomeView(ShowCreatePattern));

    public void ShowCreatePattern() => Navigate(AppRoute.CreatePattern, new CreatePatternView(async pattern =>
    {
        await _appState.BeginOnboardingWithPatternAsync(pattern);
        ShowSecurityQuestionsSetup();
    }));

    public void ShowSecurityQuestionsSetup() => Navigate(AppRoute.SecurityQuestionsSetup, new SecurityQuestionsSetupView(async (questions, answers) =>
    {
        await _appState.FinishOnboardingSecurityQuestionsAsync(questions, answers);
        ShowBiometricOptIn();
    }));

    public void ShowBiometricOptIn() => Navigate(AppRoute.BiometricOptIn, new BiometricOptInView(_appState, async enableHello =>
    {
        await _appState.FinishOnboardingAsync(enableHello);
        ShowVaultList();
    }));

    // --- Login ---

    public void ShowLogin() => Navigate(AppRoute.Login, new LoginView(_appState, ShowVaultList, ShowForgotPattern, ShowHiddenHub));

    public void ShowForgotPattern() => Navigate(AppRoute.ForgotPattern, new ForgotPatternView(_appState, ShowVaultList, ShowLogin));

    // --- Vault ---

    public void ShowVaultList() => Navigate(AppRoute.VaultList, new VaultListView(
        _appState,
        onAddEntry: () => ShowAddEditEntry(null),
        onOpenEntry: ShowEntryDetail,
        onOpenSettings: ShowSettings,
        onOpenPhotoVault: ShowPhotoVault,
        onViewLoginActivity: ShowLoginActivity,
        onLock: () => _appState.Lock()));

    public void ShowEntryDetail(Credential credential) => Navigate(AppRoute.EntryDetail, new EntryDetailView(
        _appState,
        credential,
        onBack: ShowVaultList,
        onEdit: () => ShowAddEditEntry(credential),
        onDeleted: ShowVaultList));

    public void ShowAddEditEntry(Credential? existing) => Navigate(AppRoute.AddEditEntry, new AddEditEntryView(
        existing,
        onSave: async credential =>
        {
            await _appState.VaultRepository.UpsertAsync(credential);
            ShowVaultList();
        },
        onBack: ShowVaultList));

    // --- Settings ---

    public void ShowSettings() => Navigate(AppRoute.Settings, new SettingsView(
        _appState,
        onBack: ShowVaultList,
        onChangePattern: ShowChangePattern,
        onChangeSecurityQuestions: ShowManageSecurityQuestions,
        onExportBackup: ShowBackupExport,
        onImportBackup: ShowBackupImport,
        onOpenLoginActivity: ShowLoginActivity,
        onSetUpHiddenVault: ShowHiddenVaultSetup,
        onErased: ShowWelcome));

    public void ShowHiddenVaultSetup() => Navigate(AppRoute.HiddenVaultSetup, new HiddenVaultSetupView(_appState, ShowSettings, ShowSettings));

    public void ShowHiddenHub() => Navigate(AppRoute.HiddenHub, new HiddenVaultHubView(
        onOpenPasswords: ShowHiddenPasswords,
        onOpenPhotos: ShowHiddenPhotos,
        onOpenNotes: ShowHiddenNotes,
        onLock: () => _appState.LockHiddenVault()));

    public void ShowHiddenPasswords() => Navigate(AppRoute.HiddenPasswords, new HiddenPasswordsView(
        _appState,
        onAdd: () => ShowHiddenAddEdit(null),
        onOpen: ShowHiddenEntryDetail,
        onBack: ShowHiddenHub));

    public void ShowHiddenEntryDetail(Credential credential) => Navigate(AppRoute.HiddenEntryDetail, new EntryDetailView(
        _appState,
        credential,
        onBack: ShowHiddenPasswords,
        onEdit: () => ShowHiddenAddEdit(credential),
        onDeleted: ShowHiddenPasswords,
        repository: _appState.HiddenVaultRepository));

    public void ShowHiddenAddEdit(Credential? existing) => Navigate(AppRoute.HiddenAddEdit, new AddEditEntryView(
        existing,
        onSave: async credential =>
        {
            await _appState.HiddenVaultRepository.UpsertAsync(credential);
            ShowHiddenPasswords();
        },
        onBack: ShowHiddenPasswords));

    public void ShowHiddenPhotos() => Navigate(AppRoute.HiddenPhotos, new PhotoVaultView(
        _appState, ShowHiddenHub, _appState.HiddenPhotoVaultRepository, "Secret photos"));

    public void ShowHiddenNotes() => Navigate(AppRoute.HiddenNotes, new HiddenNotesView(_appState, onLock: ShowHiddenHub));

    public void ShowChangePattern() => Navigate(AppRoute.ChangePattern, new ChangePatternView(_appState, ShowSettings, ShowSettings));

    public void ShowManageSecurityQuestions() => Navigate(AppRoute.ManageSecurityQuestions, new SecurityQuestionsSetupView(async (questions, answers) =>
    {
        await _appState.ChangeSecurityQuestionsAsync(questions, answers);
        ShowSettings();
    }));

    public void ShowBackupExport() => Navigate(AppRoute.BackupExport, new BackupExportView(_appState, ShowSettings, ShowSettings));

    public void ShowBackupImport() => Navigate(AppRoute.BackupImport, new BackupImportView(_appState, ShowSettings, ShowSettings));

    public void ShowLoginActivity() => Navigate(AppRoute.LoginActivity, new LoginActivityView(_appState.GetLoginEvents(), ShowVaultListOrSettingsBack));

    public void ShowPhotoVault() => Navigate(AppRoute.PhotoVault, new PhotoVaultView(_appState, ShowVaultList));

    // Login activity is reachable from both the vault list and Settings; just go back to the
    // vault list either way since that's always reachable and valid once unlocked.
    private void ShowVaultListOrSettingsBack() => ShowVaultList();
}
