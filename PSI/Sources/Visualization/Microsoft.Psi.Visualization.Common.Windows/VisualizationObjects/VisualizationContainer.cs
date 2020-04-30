﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows.Data;
    using GalaSoft.MvvmLight.Command;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.Serialization;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents the container where all visualization panels are hosted. The is the root UI element for visualizations.
    /// </summary>
    public class VisualizationContainer : ObservableObject
    {
        private RelayCommand<VisualizationPanel> deleteVisualizationPanelCommand;

        /// <summary>
        /// The name of the container.
        /// </summary>
        private string name;

        /// <summary>
        /// The time navigator view model.
        /// </summary>
        private Navigator navigator;

        /// <summary>
        /// The collection of visualization Panels.
        /// </summary>
        private ObservableCollection<VisualizationPanel> panels;

        /// <summary>
        /// multithreaded collection lock.
        /// </summary>
        private object panelsLock;

        /// <summary>
        /// The current visualization panel.
        /// </summary>
        private VisualizationPanel currentPanel;

        /// <summary>
        /// The current visualization object (if any) currently being snapped to.
        /// </summary>
        private VisualizationObject snapToVisualizationObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationContainer"/> class.
        /// </summary>
        public VisualizationContainer()
        {
            this.navigator = new Navigator();
            this.panels = new ObservableCollection<VisualizationPanel>();
            this.InitNew();
        }

        /// <summary>
        /// Gets or sets the current visualization panel.
        /// </summary>
        [IgnoreDataMember]
        public VisualizationPanel CurrentPanel
        {
            get { return this.currentPanel; }
            set { this.Set(nameof(this.CurrentPanel), ref this.currentPanel, value); }
        }

        /// <summary>
        /// Gets or sets the name of the container.
        /// </summary>
        [DataMember]
        public string Name
        {
            get { return this.name; }
            set { this.Set(nameof(this.Name), ref this.name, value); }
        }

        /// <summary>
        /// Gets the current navigator.
        /// </summary>
        [IgnoreDataMember]
        public Navigator Navigator
        {
            get { return this.navigator; }
            private set { this.Set(nameof(this.Navigator), ref this.navigator, (Navigator)value); }
        }

        /// <summary>
        /// Gets the visualization Panels.
        /// </summary>
        [DataMember]
        public ObservableCollection<VisualizationPanel> Panels
        {
            get { return this.panels; }
            private set { this.Set(nameof(this.Panels), ref this.panels, value); }
        }

        /// <summary>
        /// Gets or sets the visualization object that the mouse pointer currently snaps to.
        /// </summary>
        [IgnoreDataMember]
        public VisualizationObject SnapToVisualizationObject
        {
            get { return this.snapToVisualizationObject; }

            set
            {
                this.RaisePropertyChanging(nameof(this.SnapToVisualizationObject));
                this.snapToVisualizationObject = value;
                this.RaisePropertyChanged(nameof(this.SnapToVisualizationObject));
            }
        }

        /// <summary>
        /// Gets the delete visualization panel command.
        /// </summary>
        [IgnoreDataMember]
        public RelayCommand<VisualizationPanel> DeleteVisualizationPanelCommand
        {
            get
            {
                if (this.deleteVisualizationPanelCommand == null)
                {
                    this.deleteVisualizationPanelCommand = new RelayCommand<VisualizationPanel>(
                        o =>
                        {
                            this.RemovePanel(o);
                        });
                }

                return this.deleteVisualizationPanelCommand;
            }
        }

        /// <summary>
        /// Loads a visualization layout from the specified file.
        /// </summary>
        /// <param name="filename">File to load visualization layout.</param>
        /// <returns>The new visualization container.</returns>
        public static VisualizationContainer Load(string filename)
        {
            var serializer = JsonSerializer.Create(
                new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                });

            StreamReader jsonFile = null;
            try
            {
                jsonFile = File.OpenText(filename);
                using (var jsonReader = new JsonTextReader(jsonFile))
                {
                    jsonFile = null;
                    VisualizationContainer container = serializer.Deserialize<VisualizationContainer>(jsonReader);
                    return container;
                }
            }
            finally
            {
                jsonFile?.Dispose();
            }
        }

        /// <summary>
        /// Adds a new panel to the container.
        /// </summary>
        /// <param name="panel">The panel to be added to the container.</param>
        /// <param name="isRootChild">Flag indicating whether panel is root child.</param>
        public void AddPanel(VisualizationPanel panel, bool isRootChild = true)
        {
            panel.SetParentContainer(this);
            if (isRootChild)
            {
                if (this.CurrentPanel != null)
                {
                    this.Panels.Insert(this.Panels.IndexOf(this.CurrentPanel) + 1, panel);
                }
                else
                {
                    this.Panels.Add(panel);
                }
            }

            this.CurrentPanel = panel;
        }

        /// <summary>
        /// Creates and adds a new panel to the container.
        /// </summary>
        /// <typeparam name="T">The type of panel to add.</typeparam>
        /// <returns>The newly added panel.</returns>
        public T AddPanel<T>()
            where T : VisualizationPanel, new()
        {
            T panel = new T();
            this.AddPanel(panel);
            return panel;
        }

        /// <summary>
        /// Removes all Panels from the container.
        /// </summary>
        public void Clear()
        {
            foreach (var panel in this.Panels)
            {
                panel.Clear();
            }

            this.Panels.Clear();
            this.CurrentPanel = null;
        }

        /// <summary>
        /// Removes the indicated panel.
        /// </summary>
        /// <param name="panel">The panel to be removed from the container.</param>
        public void RemovePanel(VisualizationPanel panel)
        {
            // change the current panel
            if (this.CurrentPanel == panel)
            {
                this.CurrentPanel = null;
            }

            // If the panel being deleted contains the stream currently being snapped to, then reset the snap to stream object
            if ((this.snapToVisualizationObject != null) && panel.VisualizationObjects.Contains(this.snapToVisualizationObject))
            {
                this.SnapToVisualizationObject = null;
            }

            panel.Clear();
            this.Panels.Remove(panel);

            if ((this.CurrentPanel == null) && (this.Panels.Count > 0))
            {
                this.CurrentPanel = this.Panels.Last();
            }
        }

        /// <summary>
        /// Saves the current layout to the specified file.
        /// </summary>
        /// <param name="filename">The file to save the layout too.</param>
        public void Save(string filename)
        {
            var serializer = JsonSerializer.Create(
                new JsonSerializerSettings()
                {
                    ContractResolver = new Instant3DVisualizationObjectContractResolver(),
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                });

            StreamWriter jsonFile = null;
            try
            {
                jsonFile = File.CreateText(filename);
                using (var jsonWriter = new JsonTextWriter(jsonFile))
                {
                    jsonFile = null;
                    serializer.Serialize(jsonWriter, this);
                }
            }
            finally
            {
                jsonFile?.Dispose();
            }
        }

        /// <summary>
        /// Update the store bindings with the specified enumeration of partitions.
        /// </summary>
        /// <param name="session">The session currently being visualized.</param>
        public void UpdateStreamBindings(Session session)
        {
            foreach (var panel in this.Panels)
            {
                foreach (var vo in ((VisualizationPanel)panel).VisualizationObjects)
                {
                    var svo = vo as IStreamVisualizationObject;
                    svo?.UpdateStreamBinding(session);
                }
            }
        }

        /// <summary>
        /// Notifies that a partition's live status has changed.
        /// </summary>
        /// <param name="storePath">The path to the store whose live status has changed.</param>
        /// <param name="isLive">True if the partition is live, otherwise false.</param>
        public void NotifyLivePartitionStatus(string storePath, bool isLive)
        {
            // For every visualization object, if its source of data is
            // storePath, then update the visualization object's IsLive value
            foreach (VisualizationPanel panel in this.Panels)
            {
                foreach (VisualizationObject visualizationobject in panel.VisualizationObjects)
                {
                    IStreamVisualizationObject streamVisualizationObject = visualizationobject as IStreamVisualizationObject;
                    if ((streamVisualizationObject != null) && (streamVisualizationObject.StreamBinding.StorePath == storePath))
                    {
                        streamVisualizationObject.IsLive = isLive;
                    }
                }
            }
        }

        /// <summary>
        /// Zoom to the spcified time interval.
        /// </summary>
        /// <param name="timeInterval">Time interval to zoom to.</param>
        public void ZoomToRange(TimeInterval timeInterval)
        {
            this.Navigator.SelectionRange.SetRange(timeInterval.Left, timeInterval.Right);
            this.Navigator.ViewRange.SetRange(timeInterval.Left, timeInterval.Right);
            this.Navigator.Cursor = timeInterval.Left;
        }

        private void InitNew()
        {
            this.panelsLock = new object();
            BindingOperations.EnableCollectionSynchronization(this.panels, this.panelsLock);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            this.InitNew();
            foreach (var panel in this.Panels)
            {
                panel.SetParentContainer(this);
            }
        }
    }
}