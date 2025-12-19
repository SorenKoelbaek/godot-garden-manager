using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GardenManager.Managers
{
    /// <summary>
    /// Tracks loading progress across multiple phases and updates the loading UI.
    /// Makes it easy to add new loading phases (plot types, API calls, etc.)
    /// </summary>
    public class LoadingProgressTracker
    {
        private LoadingSpinner _spinner;
        private Dictionary<string, LoadingPhase> _phases = new Dictionary<string, LoadingPhase>();
        private float _totalWeight = 0.0f;
        private float _currentProgress = 0.0f;

        private class LoadingPhase
        {
            public string Name { get; set; }
            public float Weight { get; set; }
            public float Progress { get; set; } // 0.0 to 1.0
            public bool IsComplete { get; set; }
        }

        public LoadingProgressTracker(LoadingSpinner spinner)
        {
            _spinner = spinner;
        }

        /// <summary>
        /// Register a loading phase with a weight (percentage of total progress).
        /// Weights are normalized so they don't need to add up to 100.
        /// </summary>
        /// <param name="name">Unique identifier for this phase</param>
        /// <param name="weight">Weight/percentage this phase represents (e.g., 10.0 for 10%)</param>
        public void RegisterPhase(string name, float weight)
        {
            if (_phases.ContainsKey(name))
            {
                GD.PrintErr($"LoadingProgressTracker: Phase '{name}' already registered!");
                return;
            }

            _phases[name] = new LoadingPhase
            {
                Name = name,
                Weight = weight,
                Progress = 0.0f,
                IsComplete = false
            };

            _totalWeight += weight;
            GD.Print($"LoadingProgressTracker: Registered phase '{name}' with weight {weight}%");
        }

        /// <summary>
        /// Register multiple phases that should share the remaining progress equally.
        /// Useful for plot types where each plot gets equal progress.
        /// </summary>
        /// <param name="names">Array of phase names</param>
        /// <param name="totalWeight">Total weight to distribute among all phases</param>
        public void RegisterEqualPhases(string[] names, float totalWeight)
        {
            if (names == null || names.Length == 0)
            {
                GD.PrintErr("LoadingProgressTracker: Cannot register equal phases - no names provided");
                return;
            }

            float weightPerPhase = totalWeight / names.Length;
            foreach (var name in names)
            {
                RegisterPhase(name, weightPerPhase);
            }

            GD.Print($"LoadingProgressTracker: Registered {names.Length} equal phases, {weightPerPhase:F2}% each");
        }

        /// <summary>
        /// Update progress for a specific phase (0.0 to 1.0).
        /// </summary>
        public void UpdatePhaseProgress(string phaseName, float progress, string statusMessage = "")
        {
            if (!_phases.ContainsKey(phaseName))
            {
                GD.PrintErr($"LoadingProgressTracker: Phase '{phaseName}' not registered!");
                return;
            }

            var phase = _phases[phaseName];
            phase.Progress = Mathf.Clamp(progress, 0.0f, 1.0f);
            
            UpdateOverallProgress(statusMessage);
        }

        /// <summary>
        /// Mark a phase as complete (progress = 1.0).
        /// </summary>
        public void CompletePhase(string phaseName, string statusMessage = "")
        {
            if (!_phases.ContainsKey(phaseName))
            {
                GD.PrintErr($"LoadingProgressTracker: Phase '{phaseName}' not registered!");
                return;
            }

            var phase = _phases[phaseName];
            phase.Progress = 1.0f;
            phase.IsComplete = true;

            UpdateOverallProgress(statusMessage);
        }

        /// <summary>
        /// Update progress for a phase that processes multiple items.
        /// Useful for "Creating X plots" where each plot increments progress.
        /// </summary>
        /// <param name="phaseName">Phase name</param>
        /// <param name="completedItems">Number of items completed</param>
        /// <param name="totalItems">Total number of items</param>
        /// <param name="itemName">Name of current item being processed (for status message)</param>
        public void UpdateItemProgress(string phaseName, int completedItems, int totalItems, string itemName = "")
        {
            if (totalItems == 0)
            {
                CompletePhase(phaseName, $"Completed {phaseName}");
                return;
            }

            float progress = (float)completedItems / totalItems;
            string status = string.IsNullOrEmpty(itemName) 
                ? $"{phaseName}: {completedItems}/{totalItems}" 
                : $"{phaseName}: {itemName} ({completedItems}/{totalItems})";

            UpdatePhaseProgress(phaseName, progress, status);
        }

        /// <summary>
        /// Set overall progress directly (0.0 to 100.0).
        /// Use this for phases not registered in the system.
        /// </summary>
        public void SetProgress(float progress, string statusMessage = "")
        {
            _currentProgress = Mathf.Clamp(progress, 0.0f, 100.0f);
            UpdateUI(_currentProgress, statusMessage);
        }

        /// <summary>
        /// Reset all phases (useful for restarting loading).
        /// </summary>
        public void Reset()
        {
            foreach (var phase in _phases.Values)
            {
                phase.Progress = 0.0f;
                phase.IsComplete = false;
            }
            _currentProgress = 0.0f;
            UpdateUI(0.0f, "Initializing...");
        }

        private void UpdateOverallProgress(string statusMessage = "")
        {
            if (_totalWeight == 0.0f)
            {
                GD.PrintErr("LoadingProgressTracker: No phases registered, cannot calculate progress!");
                return;
            }

            // Calculate weighted progress
            float weightedProgress = 0.0f;
            foreach (var phase in _phases.Values)
            {
                weightedProgress += (phase.Weight / _totalWeight) * 100.0f * phase.Progress;
            }

            _currentProgress = weightedProgress;
            UpdateUI(_currentProgress, statusMessage);
        }

        private void UpdateUI(float progress, string statusMessage)
        {
            if (_spinner != null)
            {
                _spinner.SetProgress(progress);
                if (!string.IsNullOrEmpty(statusMessage))
                {
                    _spinner.SetStatus(statusMessage);
                }
            }
        }

        /// <summary>
        /// Get current progress (0.0 to 100.0).
        /// </summary>
        public float CurrentProgress => _currentProgress;

        /// <summary>
        /// Check if all registered phases are complete.
        /// </summary>
        public bool IsComplete => _phases.Values.All(p => p.IsComplete);
    }
}
