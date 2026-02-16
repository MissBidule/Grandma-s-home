using System;
using System.Collections.Generic;
using PurrNet.Logging;
using UnityEngine;

namespace PurrNet.Voice
{
    public partial class PurrVoicePlayer
    {
        [SerializeField] private SyncFilters _audioFilters = new();
        [SerializeField] private bool _enableSmoothing = true;
        
        public List<SyncFilters.Filter> audioFilters => _audioFilters.ToList();
        private SyncFilters _localFilters;
        
        /// <summary>
        /// This adds a filter to the audio processing chain.
        /// </summary>
        /// <param name="filter">Filter to add</param>
        /// <param name="level">Level at which the filter should be processed</param>
        /// <param name="initialStrength">The strength of the filter that gets setup. Only at this point can you sync the strength</param>
        public void AddFilter(PurrAudioFilter filter, FilterLevel level, float initialStrength = 1)
        {
            _audioFilters.AddFilter(filter, level, initialStrength);
            _localFilters.AddFilter(filter, level, initialStrength);
        }

        /// <summary>
        /// Removed a filter from the audio processing chain.
        /// </summary>
        /// <param name="index">Index at which to remove said filter</param>
        public void RemoveFilter(int index)
        {
            _audioFilters.RemoveFilter(index);
            _localFilters.RemoveFilter(index);
        }

        /// <summary>
        /// This removes a filter from the audio processing chain.
        /// </summary>
        /// <param name="filter">The filter you wish to remove. If multiple, it'll remove the first found</param>
        public void RemoveFilter(PurrAudioFilter filter)
        {
            var filterIndex = _audioFilters.ToList().FindIndex(f => f.audioFilter == filter);
            if (filterIndex >= 0)
                RemoveFilter(filterIndex);
            else
                PurrLogger.LogError($"Filter {filter.name} not found in the audio filters list.");
        }
        
        /// <summary>
        /// Sets the strength of a filter at a specific index. This only happens locally, so you need to sync it manually if you want it to be reflected on other clients.
        /// </summary>
        /// <param name="index">Index of filter</param>
        /// <param name="strength">Strength to set</param>
        public void SetFilterStrength(int index, float strength)
        {
            if (index < 0 || index >= _audioFilters.Count)
            {
                PurrLogger.LogError($"Invalid filter index: {index}. Cannot set strength.");
                return;
            }

            var filter = _audioFilters[index];
            filter.strength = strength;
            _audioFilters[index] = filter;
    
            var localFilter = _localFilters[index];
            localFilter.strength = strength;
            _localFilters[index] = localFilter;
        }
        
        /// <summary>
        /// Sets the strength of a specific filter. This only happens locally, so you need to sync it manually if you want it to be reflected on other clients.
        /// </summary>
        /// <param name="filter">Filter you want to change the strength of</param>
        /// <param name="strength">Strength to set</param>
        public void SetFilterStrength(PurrAudioFilter filter, float strength)
        {
            var filterIndex = _audioFilters.ToList().FindIndex(f => f.audioFilter == filter);
            if (filterIndex >= 0)
                SetFilterStrength(filterIndex, strength);
            else
                PurrLogger.LogError($"Filter {filter.name} not found in the audio filters list.");
        }
        
        private void FilterAwake()
        {
            _localFilters = new(_audioFilters);
            
            for (var i = 0; i < _audioFilters.Count; i++)
            {
                var f = _audioFilters[i];
                f.Init();
                _audioFilters[i] = f;
            }
        }

        private ArraySegment<float> DoProcessFilters(ArraySegment<float> inputSamples, int frequency, params FilterLevel[] levels)
        {
            return ProcessFilters(_audioFilters, inputSamples, frequency, levels);
        }

        private ArraySegment<float> DoLocalProcessFilters(ArraySegment<float> inputSamples, int frequency, params FilterLevel[] levels)
        {
            return ProcessFilters(_localFilters, inputSamples, frequency, levels);
        }
        
        private ArraySegment<float> ProcessFilters(SyncFilters filters, ArraySegment<float> inputSamples, int frequency, params FilterLevel[] levels)
        {
            if(_enableSmoothing)
                inputSamples = HandleSmoothing(inputSamples);
            
            if (filters.Count == 0)
                return inputSamples;

            ArraySegment<float> processedSamples = inputSamples;
            for (var i = 0; i < levels.Length; i++)
            {
                var level = levels[i];
                for (var x = 0; x < filters.Count; x++)
                {
                    var filter = filters[x];
                    filter.Process(processedSamples, frequency, level);
                }
            }

            return processedSamples;
        }

        private ArraySegment<float> HandleSmoothing(ArraySegment<float> inputSamples)
        {
            //Smooth gain by applying a tanh function
            if (inputSamples.Array == null)
                return inputSamples;
         
            for (int i = 0; i < inputSamples.Count; i++)
            {
                float sample = inputSamples.Array[inputSamples.Offset + i];
                inputSamples.Array[inputSamples.Offset + i] = (float)Math.Tanh(sample * 2f);
            }

            return inputSamples;
        }
    }
}
