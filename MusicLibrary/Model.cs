﻿//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------
using System;
using MusicLibrary;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.ApplicationModel.VoiceCommands;

namespace MusicLibrary
{
    /// <summary>
    /// Provides the data model for the MusicLibrary app.
    /// </summary>
    [DataContract]
    public class Model
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Model"/> class.
        /// </summary>
        public Model()
        {
            Initialize();
        }

        /// <summary>
        /// Initialization is called by the constructor or OnDeserializing() to initialize the model.
        /// Deserializing an object using DataContractJsonSerializer doesn't call the constructor
        /// so we hook the deserializing process and initialize our object here.
        /// </summary>
        private void Initialize()
        {
            this._family = new ObservableCollection<Person>();
            this._songList = new ObservableCollection<Song>();
        }


        /// <summary>
        /// The collection of people in the family that may have songs displayed
        /// </summary>
        [DataMember]
        public ObservableCollection<Person> Family
        {
            get
            {
                return _family;
            }
        }

        /// <summary>
        /// All of the songs
        /// </summary>
        [DataMember]
        public ObservableCollection<Song> SongList
        {
            get
            {
                return this._songList;
            }
        }

        /// <summary>
        /// Adds a person to the family collection and updates speech
        /// to be aware of their name.
        /// </summary>
        /// <param name="name">The name of the new family member</param>
        /// <param name="pathToImage">Path to an image that represents them</param>
        public async void AddPersonAsync(string name, string pathToImage)
        {
            _family.Add(new Person(name, pathToImage));
            await UpdateVCDPhraseList();
        }

        /// <summary>
        /// Adds a person to the family collection and updates speech
        /// to be aware of their name.
        /// </summary>
        /// <param name="newPerson"></param>
        public async Task AddPersonAsync(Person newPerson)
        {
            _family.Add(newPerson);
            await UpdateVCDPhraseList();
        }

        /// <summary>
        /// Delete the data associated with a Person which
        /// includes the person's songs and the information
        /// stored in the Users\{person's name} folder
        /// </summary>
        /// <param name="name">Name of the person to delete</param>
        /// <returns></returns>
        public async Task<bool> DeletePersonAsync(string name)
        {
            if (name != App.EVERYONE) // Everyone is a permanent user so don't delete them
            {
                await MusicLibrary.UserDetection.FacialSimilarity.DeleteFaceFromUserFaceListAsync(name);
            }

            // Remove all the songs for this person
            foreach (var song in SongList.Where(n => n.SongIsFor.FriendlyName == name).ToList())
            {
                DeleteSong(song);
            }

            // Remove the user directory for this person.
            bool success = true;
            if (name != App.EVERYONE) // the everyone user is permanent. We'll delete the songs for Everyone but not the actual person named 'Everyone'
            {
                success = await IsolatedStorageFile.GetUserStoreForApplication().DeleteDirectoryAsync($@"Users\{name}"); // uses extension method found in Utils.cs
                this.Family.Remove(PersonFromName(name)); // Speech listens to changes on this collection and will update grammar, etc.
            }

            return success;
        }

        /// <summary>
        /// Get the Person object for the specified name
        /// </summary>
        /// <param name="name">The case-insensitive name of the person</param>
        /// <returns></returns>
        public Person PersonFromName(string name) => Family.FirstOrDefault(person => person.FriendlyName.Equals(name, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Add a song to the collection of songs
        /// </summary>
        /// <param name="contents">The text contents of the songs (if any)</param>
        /// <param name="nameTag">The person for whom this song is for</param>
        public void AddSong(string contents, string nameTag)  => SongList.Add(new Song(nameTag));


        /// <summary>
        /// Delete a song from the collection of songs
        /// </summary>
        /// <param name="songToDelete"></param>
        public void DeleteSong(Song songToDelete) => SongList.Remove(songToDelete);

        /// <summary>
        /// Create a default collection people.
        /// </summary>
        public void CreateDefaultFamily()
        {
            AddPersonAsync(App.EVERYONE, "/Assets/face_1.png"); // Note: "Everyone" is special, and should always be added.
            // You can add other default family members here.
        }

        /// <summary>
        /// Update the content of the "person" PhraseList element in the VCD 
        /// when the app is launched or a person is added.
        /// </summary>
        public async Task UpdateVCDPhraseList()
        {
            try
            {
                VoiceCommandDefinition commandDefinitions;
                
                // We only support one locale (en-US) in the VCD.
                // Use System.Globalization.CultureInfo to support additional locales.
                string countryCode = CultureInfo.CurrentCulture.Name.ToLower();
                if (countryCode.Length == 0)
                {
                    countryCode = "en-us";
                }

                if (VoiceCommandDefinitionManager.InstalledCommandDefinitions.TryGetValue(
                    "MyNotesCommandSet_" + countryCode, out commandDefinitions))
                {
                    System.Collections.Generic.List<string> _friendlyName = new List<string>();

                    foreach (Person _person in Family)
                    {
                        _friendlyName.Add(_person.FriendlyName);
                    }

                    await commandDefinitions.SetPhraseListAsync("person", _friendlyName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Update PhraseList element in VCD: " + ex.ToString());
            }
        }

        /// <summary>
        /// When deserializing the model, the ctor is not called.
        /// This callback allows us to initialize the model during
        /// deserialization.
        /// </summary>
        /// <param name="context"></param>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            Initialize();
        }

        private ObservableCollection<Person> _family; // All of the family members.
        private ObservableCollection<Song> _songList; // All of the songs in the app.

    }
}
