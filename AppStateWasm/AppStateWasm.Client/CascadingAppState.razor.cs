﻿using Microsoft.AspNetCore.Components;
using Blazored.LocalStorage;
using System.Text.Json;

namespace AppStateWasm.Client;

public partial class CascadingAppState : ComponentBase, IAppState
{
	private readonly string StorageKey = "MyAppStateKey";

	private readonly int StorageTimeoutInSeconds = 30;

	bool loaded = false;

	public DateTime LastStorageSaveTime { get; set; }

	[Inject]
	ILocalStorageService localStorage { get; set; }

	[Parameter]
	public RenderFragment ChildContent { get; set; }

	// Alternative property change notification event
	public event Action<string> PropertyChanged;
	private void NotifyPropertyChanged(string value) => PropertyChanged?.Invoke(value);

	// Used for tracking changes
	public IAppState GetCopy()
	{
		var state = (IAppState)this;
		var json = JsonSerializer.Serialize(state);
		var copy = JsonSerializer.Deserialize<AppState>(json);
		return copy;
	}

	/// <summary>
	/// Implement property handlers like so
	/// </summary>
	private string message = "";
	public string Message
	{
		get => message;
		set
		{
			message = value;
			// Force a re-render
			StateHasChanged();
			// Raise the PropertyChanged event:
			NotifyPropertyChanged("Message");
			// Save to local storage
			new Task(async () =>
			{
				await Save();
			}).Start();
		}
	}

	private int count = 0;
	public int Count
	{
		get => count;
		set
		{
			count = value;
			StateHasChanged();
			NotifyPropertyChanged("Count");
			new Task(async () =>
			{
				await Save();
			}).Start();
		}
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await Load();
			loaded = true;
			StateHasChanged();
		}
	}

	protected override void OnInitialized()
	{
		Message = "Initial Message";
	}

	public async Task Save()
	{
		if (!loaded) return;

		// set LastSaveTime
		LastStorageSaveTime = DateTime.Now;
		// serialize 
		var state = (IAppState)this;
		// save
		await localStorage.SetItemAsync<IAppState>(StorageKey, state);
	}

	public async Task Load()
	{
		try
		{
			var json = await localStorage.GetItemAsStringAsync(StorageKey);
			if (json == null || json.Length == 0) return;
			var state = JsonSerializer.Deserialize<AppState>(json);
			if (state != null)
			{
				if (DateTime.Now.Subtract(state.LastStorageSaveTime).TotalSeconds <= StorageTimeoutInSeconds)
				{
					// decide whether to set properties manually or with reflection

					// comment to set properties manually
					//this.Message = state.Message;
					//this.Count = state.Count;

					// set properties using Reflection
					var t = typeof(IAppState);
					var props = t.GetProperties();
					foreach (var prop in props)
					{
						if (prop.Name != "LastStorageSaveTime")
						{
							object value = prop.GetValue(state);
							prop.SetValue(this, value, null);
						}
					}

				}
			}
		}
		catch (Exception ex)
		{

		}
	}
}