﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace SongLoaderPlugin.OverrideClasses
{
	public class CustomLevel : LevelSO, IScriptableObjectResetable
	{	
		public CustomSongInfo customSongInfo { get; private set; }
		public bool AudioClipLoading { get; set; }
		public bool BPMAndNoteSpeedFixed { get; private set; }
		
		public void Init(CustomSongInfo newCustomSongInfo)
		{
			customSongInfo = newCustomSongInfo;
			_levelID = customSongInfo.GetIdentifier();
			_songName = customSongInfo.songName;
			_songSubName = customSongInfo.songSubName;
			_songAuthorName = customSongInfo.GetSongAuthor();
			_beatsPerMinute = customSongInfo.beatsPerMinute;
			_songTimeOffset = customSongInfo.songTimeOffset;
			_shuffle = customSongInfo.shuffle;
			_shufflePeriod = customSongInfo.shufflePeriod;
			_previewStartTime = customSongInfo.previewStartTime;
			_previewDuration = customSongInfo.previewDuration;
			_environmentSceneInfo = EnvironmentsLoader.GetSceneInfo(customSongInfo.environmentName);
		}

		public void SetAudioClip(AudioClip newAudioClip)
		{
			_audioClip = newAudioClip;
		}

		public void SetCoverImage(Sprite newCoverImage)
		{
			_coverImage = newCoverImage;
		}

		public void SetDifficultyBeatmaps(DifficultyBeatmap[] newDifficultyBeatmaps)
		{
			_difficultyBeatmaps = newDifficultyBeatmaps;
		}

		public void SetBeatmapCharacteristics(BeatmapCharacteristicSO[] newBeatmapCharacteristics)
		{
			_beatmapCharacteristics = newBeatmapCharacteristics;
		}

		public void FixBPMAndGetNoteJumpMovementSpeed()
		{
			if (BPMAndNoteSpeedFixed) return;
			var bpms = new Dictionary<float, int> {{_beatsPerMinute, 0}};
			foreach (var diffLevel in customSongInfo.difficultyLevels)
			{
				if (string.IsNullOrEmpty(diffLevel.json)) continue;
				float? bpm, noteSpeed;
				GetBPMAndNoteJump(diffLevel.json, out bpm, out noteSpeed);

				if (bpm.HasValue)
				{
					if (bpms.ContainsKey(bpm.Value))
					{
						bpms[bpm.Value]++;
					}
					else
					{
						bpms.Add(bpm.Value, 1);
					}
				}

				if (!noteSpeed.HasValue) return;
				var diffBeatmap = _difficultyBeatmaps.FirstOrDefault(x =>
					diffLevel.difficulty.ToEnum(BeatmapDifficulty.Normal) == x.difficulty);
				var customBeatmap = diffBeatmap as CustomDifficultyBeatmap;
				if (customBeatmap == null) continue;
				customBeatmap.SetNoteJumpMovementSpeed(noteSpeed.Value);
			}

			_beatsPerMinute = bpms.OrderByDescending(x => x.Value).First().Key;

			foreach (var difficultyBeatmap in _difficultyBeatmaps)
			{
				var customBeatmap = difficultyBeatmap as CustomDifficultyBeatmap;
				if (customBeatmap == null) continue;
				customBeatmap.BeatmapDataSO.SetRequiredDataForLoad(_beatsPerMinute, _shuffle, _shufflePeriod);
				customBeatmap.BeatmapDataSO.Load();
			}

			BPMAndNoteSpeedFixed = true;
		}

		//This is quicker than using a JSON parser
		private void GetBPMAndNoteJump(string json, out float? bpm, out float? noteJumpSpeed)
		{
			bpm = null;
			noteJumpSpeed = null;
			var split = json.Split(':');
			for (var i = 0; i < split.Length; i++)
			{
				if (split[i].Contains("_beatsPerMinute"))
				{
					bpm = Convert.ToSingle(split[i + 1].Split(',')[0], CultureInfo.InvariantCulture);
				}
				
				if (split[i].Contains("_noteJumpSpeed"))
				{
					noteJumpSpeed = Convert.ToSingle(split[i + 1].Split(',')[0], CultureInfo.InvariantCulture);
				}
			}
		}
		
		public class CustomDifficultyBeatmap : DifficultyBeatmap
		{
			public CustomDifficultyBeatmap(IBeatmapLevel parentLevel, BeatmapDifficulty difficulty, int difficultyRank, float noteJumpMovementSpeed, BeatmapDataSO beatmapData) : base(parentLevel, difficulty, difficultyRank, noteJumpMovementSpeed, beatmapData)
			{
			}

			public CustomLevel customLevel
			{
				get { return level as CustomLevel; }
			}

			public CustomBeatmapDataSO BeatmapDataSO
			{
				get { return _beatmapData as CustomBeatmapDataSO; }
			}

			public void SetNoteJumpMovementSpeed(float newNoteJumpMovementSpeed)
			{
				_noteJumpMovementSpeed = newNoteJumpMovementSpeed;
			}
		}

		public void Reset()
		{
			_audioClip = null;
			BPMAndNoteSpeedFixed = false;
		}
	}
}
