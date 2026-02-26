using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZKNotes.Models;

namespace ZKNotes.ViewModels;

public sealed class GraphNode
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Snippet { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = [];
    public double X { get; set; }
    public double Y { get; set; }
    public double VelocityX { get; set; }
    public double VelocityY { get; set; }
}

public sealed class GraphEdge
{
    public string SourceId { get; init; } = string.Empty;
    public string TargetId { get; init; } = string.Empty;
}

/// <summary>
/// ViewModel for the interactive force-directed graph visualization.
/// </summary>
public partial class GraphViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly Random _rng = new();
    private CancellationTokenSource? _layoutCts;

    // Force-directed layout parameters
    private const double RepulsionForce = 5000.0;
    private const double AttractionForce = 0.01;
    private const double Damping = 0.85;
    private const double MinDistance = 30.0;
    private const double IdealEdgeLength = 150.0;

    [ObservableProperty]
    private ObservableCollection<GraphNode> _nodes = [];

    [ObservableProperty]
    private ObservableCollection<GraphEdge> _edges = [];

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private double _panX;

    [ObservableProperty]
    private double _panY;

    public GraphViewModel(MainViewModel main)
    {
        _main = main;
    }

    /// <summary>
    /// Builds graph data from the current note collection.
    /// </summary>
    [RelayCommand]
    public async Task BuildGraphAsync()
    {
        // Cancel any existing layout computation
        _layoutCts?.Cancel();
        _layoutCts = new CancellationTokenSource();
        var token = _layoutCts.Token;

        var notes = _main.Notes.ToList();

        // Notes can share the same title (e.g. multiple "Untitled Note" drafts). Build a
        // case-insensitive title -> id lookup that won't throw on duplicates.
        var titleToId = notes
            .Where(n => !string.IsNullOrWhiteSpace(n.Title))
            .GroupBy(n => n.Title.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(n => n.LastEdit).First().Id,
                StringComparer.OrdinalIgnoreCase);

        var idSet = new HashSet<string>(notes.Select(n => n.Id));

        var nodes = new List<GraphNode>();
        var edges = new List<GraphEdge>();

        foreach (var note in notes)
        {
            nodes.Add(new GraphNode
            {
                Id = note.Id,
                Title = note.Title,
                Snippet = note.Snippet,
                Tags = note.Tags,
                X = _rng.NextDouble() * 800,
                Y = _rng.NextDouble() * 600
            });

            foreach (var link in note.Links)
            {
                // Link can be a title or an ID
                string? targetId = null;
                if (idSet.Contains(link))
                    targetId = link;
                else if (titleToId.TryGetValue(link, out var id))
                    targetId = id;

                if (targetId is not null)
                {
                    edges.Add(new GraphEdge { SourceId = note.Id, TargetId = targetId });
                }
            }
        }

        Nodes = new ObservableCollection<GraphNode>(nodes);
        Edges = new ObservableCollection<GraphEdge>(edges);

        // Run initial layout iterations in background
        await Task.Run(() =>
        {
            for (int i = 0; i < 100 && !token.IsCancellationRequested; i++)
            {
                StepLayout();
            }
        }, token).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs one iteration of the force-directed layout algorithm.
    /// </summary>
    public void StepLayout()
    {
        var nodeList = Nodes.ToList();
        if (nodeList.Count < 2) return;

        var nodeMap = nodeList.ToDictionary(n => n.Id);

        // Repulsion between all node pairs
        for (int i = 0; i < nodeList.Count; i++)
        {
            for (int j = i + 1; j < nodeList.Count; j++)
            {
                var a = nodeList[i];
                var b = nodeList[j];

                double dx = b.X - a.X;
                double dy = b.Y - a.Y;
                double dist = Math.Max(Math.Sqrt(dx * dx + dy * dy), MinDistance);
                double force = RepulsionForce / (dist * dist);

                double fx = (dx / dist) * force;
                double fy = (dy / dist) * force;

                a.VelocityX -= fx;
                a.VelocityY -= fy;
                b.VelocityX += fx;
                b.VelocityY += fy;
            }
        }

        // Attraction along edges
        foreach (var edge in Edges)
        {
            if (!nodeMap.TryGetValue(edge.SourceId, out var source) ||
                !nodeMap.TryGetValue(edge.TargetId, out var target))
                continue;

            double dx = target.X - source.X;
            double dy = target.Y - source.Y;
            double dist = Math.Max(Math.Sqrt(dx * dx + dy * dy), MinDistance);
            double displacement = dist - IdealEdgeLength;
            double force = AttractionForce * displacement;

            double fx = (dx / dist) * force;
            double fy = (dy / dist) * force;

            source.VelocityX += fx;
            source.VelocityY += fy;
            target.VelocityX -= fx;
            target.VelocityY -= fy;
        }

        // Apply velocities with damping
        foreach (var node in nodeList)
        {
            node.VelocityX *= Damping;
            node.VelocityY *= Damping;
            node.X += node.VelocityX;
            node.Y += node.VelocityY;
        }
    }

    [RelayCommand]
    private void OpenNode(string? nodeId)
    {
        if (nodeId is null) return;
        var note = _main.Notes.FirstOrDefault(n => n.Id == nodeId);
        if (note is not null)
            _main.SelectNote(note);
    }

    [RelayCommand]
    private void ZoomIn() => ZoomLevel = Math.Min(ZoomLevel * 1.2, 5.0);

    [RelayCommand]
    private void ZoomOut() => ZoomLevel = Math.Max(ZoomLevel / 1.2, 0.2);

    [RelayCommand]
    private void ResetView()
    {
        ZoomLevel = 1.0;
        PanX = 0;
        PanY = 0;
    }
}
