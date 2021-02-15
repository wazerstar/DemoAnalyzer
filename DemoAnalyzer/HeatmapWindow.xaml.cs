﻿using DemoAnalyzer.Data;
using DemoAnalyzer.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DemoAnalyzer
{
    /// <summary>
    /// Interaction logic for HeatmapWindow.xaml
    /// </summary>
    public partial class HeatmapWindow : Window
    {
        private CancellationTokenSource _cts;
        private CancellationToken _ct;
        private Task _task;
        private DemoData demo;
        private HashSet<int> selectedPlayers;
        private int selectionStart;
        private int selectionEnd;
        private Minimap minimap;

        public HeatmapWindow(CancellationTokenSource cts, DemoData demo, HashSet<int> selectedPlayers, int selectionStart, int selectionEnd, Minimap minimap)
        {
            InitializeComponent();

            this.demo = demo;
            this.selectedPlayers = selectedPlayers;
            this.selectionStart = selectionStart;
            this.selectionEnd = selectionEnd;
            this.minimap = minimap;

            minimapImage.Source = Minimap.GetMinimapBackground(demo.MapName);

            _cts = cts;
            _ct = cts.Token;
            _task = Task.Run(() =>
            {
                using (var heatmap = new Heatmap(1024, 1024))
                using (var stroke = new HeatmapStamp(32))
                {
                    for (int i = selectionStart; i <= selectionEnd; i++)
                    {
                        _ct.ThrowIfCancellationRequested();

                        foreach (var player in demo.ReadPlayerInfos(i))
                        {
                            if (!selectedPlayers.Contains(player.EntityID))
                                continue;

                            var realPos = minimap.WorldSpaceToScreenSpace(new Vector(player.Position.PositionX, player.Position.PositionY));

                            heatmap.AddPoint((int)realPos.X, (int)realPos.Y, stroke);
                        }

                        var percentage = (double)(i - selectionStart) / (selectionEnd - selectionStart);

                        Dispatcher.Invoke(() =>
                        {
                            progressBar.Value = percentage;
                        });
                    }

                    Dispatcher.Invoke(() =>
                    {
                        heatmapImage.Source = heatmap.CreateHeatmap();
                    });
                }


            }, _ct);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _cts.Cancel();

            try
            {
                _task.Wait();
            }
            catch
            {

            }

            base.OnClosing(e);
        }
    }
}