﻿using System;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using sxlib;
using sxlib.Specialized;
using SynUI.Properties;

namespace SynUI.Services;

public interface ISynapseService
{
    SxLibWPF? Api { get; }
    SxLibBase.SynLoadEvents LoadState { get; }
    SxLibBase.SynAttachEvents AttachState { get; }

    RelayCommand<string> ExecuteCommand { get; }
    RelayCommand AttachCommand { get; }

    void Initialize();
}

public class SynapseService : ObservableObject, ISynapseService
{
    private SxLibBase.SynAttachEvents _attachState;
    private SxLibBase.SynLoadEvents _loadState;

    public SynapseService()
    {
        ExecuteCommand = new RelayCommand<string>(
            script => Api?.Execute(script),
            _ => AttachState is
                SxLibBase.SynAttachEvents.READY or
                SxLibBase.SynAttachEvents.ALREADY_INJECTED);

    AttachCommand = new RelayCommand(
            () => Api?.Attach(),
            () => LoadState == SxLibBase.SynLoadEvents.READY);
    }

    public RelayCommand<string> ExecuteCommand { get; }
    public RelayCommand AttachCommand { get; }
    
    public SxLibWPF? Api { get; private set; }

    public SxLibBase.SynLoadEvents LoadState
    {
        get => _loadState;
        private set
        {
            SetProperty(ref _loadState, value);
            ExecuteCommand.NotifyCanExecuteChanged();
            AttachCommand.NotifyCanExecuteChanged();
        }
    }

    public SxLibBase.SynAttachEvents AttachState
    {
        get => _attachState;
        private set
        {
            SetProperty(ref _attachState, value);
            ExecuteCommand.NotifyCanExecuteChanged();
            AttachCommand.NotifyCanExecuteChanged();
        }
    }

    public void Initialize()
    {
        Api = SxLib.InitializeWPF(Application.Current.MainWindow, Directory.GetCurrentDirectory());
        Api.LoadEvent += _sxlib_OnLoadEvent;
        Api.AttachEvent += _sxlib_OnAttachEvent;

        // Add autoexec script
        var autoexecPath = Path.Combine(Directory.GetCurrentDirectory(), "autoexec",
            "THIS FILE IS GENERATED BY SYNUI DO NOT REMOVE.lua");
        if (!File.Exists(autoexecPath))
            File.WriteAllBytes(autoexecPath, Resources.synui_auto_exec);

        Api.Load();
    }

    private void _sxlib_OnAttachEvent(SxLibBase.SynAttachEvents @event, object param) =>
        Application.Current.Dispatcher.Invoke(() => AttachState = @event);

    private void _sxlib_OnLoadEvent(SxLibBase.SynLoadEvents @event, object param) =>
        Application.Current.Dispatcher.Invoke(() => LoadState = @event);
}