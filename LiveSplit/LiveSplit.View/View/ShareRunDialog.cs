﻿using LiveSplit.Model;
using LiveSplit.Options;
using LiveSplit.TimeFormatters;
using LiveSplit.Web.Share;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiveSplit.View
{
    public partial class ShareRunDialog : Form
    {
        public IRun Run { get; set; }
        public LiveSplitState State { get; set; }
        public ISettings Settings { get; set; }
        public Func<Image> ScreenShotFunction { get; set; }

        protected IRunUploadPlatform CurrentPlatform { get; set; }

        public ShareRunDialog(LiveSplitState state, ISettings settings, Func<Image> screenShotFunction)
        {
            State = state;
            if (State.CurrentPhase != TimerPhase.Ended)
                Run = state.Run;
            else
            {
                var model = new TimerModel();
                model.CurrentState = State;
                model.SetRunAsPB();
                Run = State.Run;
            }
            ScreenShotFunction = screenShotFunction;
            Settings = settings;
            InitializeComponent();
        }

        void RefreshCategoryList(String gameName)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    String[] categoryNames;
                    try
                    {
                        categoryNames = CurrentPlatform.GetGameCategories(CurrentPlatform.GetGameIdByName(gameName)).Select(x => x.Value).ToArray();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);

                        categoryNames = new String[0];
                    }
                    Action invokation = () =>
                    {
                        try
                        {
                            cbxCategory.Items.Clear();
                            cbxCategory.Items.AddRange(categoryNames);
                            cbxCategory.SelectedItem = categoryNames.FindMostSimilarValueTo(Run.CategoryName);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex);
                        }
                    };
                    if (this.InvokeRequired)
                        this.Invoke(invokation);
                    else
                        invokation();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            });
        }

        void RefreshGameList()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    String[] gameNames;
                    try
                    {
                        gameNames = CurrentPlatform.GetGameNames().ToArray();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);

                        gameNames = new String[0];
                    }
                    Action invokation = () =>
                    {
                        try
                        {
                            cbxGame.Items.Clear();
                            cbxGame.Items.AddRange(gameNames);
                            var selectedGameName = gameNames.FindMostSimilarValueTo(Run.GameName);
                            cbxGame.SelectedItem = selectedGameName;
                            RefreshCategoryList(selectedGameName);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex);
                        }
                    };
                    if (this.InvokeRequired)
                        this.Invoke(invokation);
                    else
                        invokation();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            });
        }

        private void SubmitDialog_Load(object sender, EventArgs e)
        {
            cbxPlatform.Items.Add("Twitter");
            cbxPlatform.Items.Add("Twitch");
            if (State.CurrentPhase == TimerPhase.Ended
                || (State.CurrentPhase == TimerPhase.NotRunning
                && State.Run.Last().PersonalBestSplitTime[State.CurrentTimingMethod] != null))
            {
                cbxPlatform.Items.Add("PBTracker");
                cbxPlatform.Items.Add("AllSpeedRuns");
            }
            if (State.CurrentPhase == TimerPhase.Ended)
            {
                cbxPlatform.Items.Add("Congratsio");
            }
            if (State.CurrentPhase == TimerPhase.NotRunning || State.CurrentPhase == TimerPhase.Ended)
            {
                cbxPlatform.Items.Add("Splits.io");
                //cbxPlatform.Items.Add("Ge.tt");
            }

            cbxPlatform.Items.Add("Screenshot");
            cbxPlatform.Items.Add("Imgur");
            cbxPlatform.SelectedIndex = 0;
            cbxPlatform_SelectionChangeCommitted(null, null);
        }

        private void RefreshDescription()
        {
            lblDescription.Text = CurrentPlatform.Description;
        }

        private void cbxGame_SelectionChangeCommitted(object sender, EventArgs e)
        {
            RefreshCategoryList(cbxGame.SelectedItem.ToString());
        }

        private void cbxPlatform_SelectionChangeCommitted(object sender, EventArgs e)
        {
            cbxGame.Items.Clear();
            cbxCategory.Items.Clear();
            cbxGame.SelectedItem = null;
            cbxCategory.SelectedItem = null;

            switch (cbxPlatform.SelectedItem.ToString())
            {
                case "PBTracker": CurrentPlatform = PBTracker.Instance; break;
                case "AllSpeedRuns": CurrentPlatform = AllSpeedRuns.Instance; break;
                case "Splits.io": CurrentPlatform = SplitsIO.Instance; break;
                case "Ge.tt": CurrentPlatform = Gett.Instance; break;
                case "Twitter": CurrentPlatform = Twitter.Instance; break;
                case "Twitch": CurrentPlatform = Twitch.Instance; break;
                case "Congratsio": CurrentPlatform = Congratsio.Instance; break;
                case "Screenshot": CurrentPlatform = Screenshot.Instance; break;
                case "Imgur": CurrentPlatform = Imgur.Instance; break;
            }

            CurrentPlatform.Settings = Settings;

            txtNotes.Enabled = btnInsertCategory.Enabled = btnInsertDeltaTime.Enabled = btnInsertGame.Enabled
                = btnInsertPB.Enabled = btnInsertSplitName.Enabled = btnInsertSplitTime.Enabled
                = btnInsertStreamLink.Enabled = btnInsertTitle.Enabled = btnPreview.Enabled =
                ((CurrentPlatform == Twitter.Instance || CurrentPlatform == Twitch.Instance || CurrentPlatform == Imgur.Instance) |
                (txtUser.Enabled = ((CurrentPlatform == Congratsio.Instance) | (txtPassword.Enabled =
                txtVersion.Enabled = cbxCategory.Enabled = cbxGame.Enabled = 
                (CurrentPlatform == PBTracker.Instance || CurrentPlatform == AllSpeedRuns.Instance)))));

            if (State.CurrentPhase == TimerPhase.NotRunning || State.CurrentPhase == TimerPhase.Ended)
                chkAttachSplits.Enabled = !(CurrentPlatform == Screenshot.Instance || CurrentPlatform == SplitsIO.Instance
                    || CurrentPlatform == Twitch.Instance);
            else
                chkAttachSplits.Enabled = false;

            if (State.CurrentPhase == TimerPhase.Ended || State.CurrentPhase == TimerPhase.NotRunning
                || State.CurrentSplitIndex == 0)
            {
                btnInsertDeltaTime.Enabled = btnInsertSplitName.Enabled = btnInsertSplitTime.Enabled = false;
            }

            if (State.Run.Last().PersonalBestSplitTime[State.CurrentTimingMethod] == null)
                btnInsertPB.Enabled = false;
            if (String.IsNullOrEmpty(State.Run.GameName))
                btnInsertGame.Enabled = false;
            if (String.IsNullOrEmpty(State.Run.CategoryName))
                btnInsertCategory.Enabled = false;
            if (String.IsNullOrEmpty(State.Run.GameName) && String.IsNullOrEmpty(State.Run.CategoryName))
                btnInsertTitle.Enabled = false;
            txtVideoURL.Enabled = 
                CurrentPlatform == PBTracker.Instance 
                || CurrentPlatform == AllSpeedRuns.Instance 
                || CurrentPlatform == Congratsio.Instance;

            RefreshGameList();
            RefreshDescription();
            RefreshNotes();
        }

        private String FormatNotes(String notePlaceholder)
        {
            var timeFormatter = new RegularTimeFormatter(TimeAccuracy.Seconds);
            var deltaTimeFormatter = new DeltaTimeFormatter();

            var game = Run.GameName ?? "";
            var category = Run.CategoryName ?? "";
            var pb = timeFormatter.Format(Run.Last().PersonalBestSplitTime[State.CurrentTimingMethod]) ?? "";

            var titleBuilder = new StringBuilder();
            var gameNameEmpty = String.IsNullOrEmpty(Run.GameName);
            var categoryEmpty = String.IsNullOrEmpty(Run.CategoryName);

            if (!gameNameEmpty || !categoryEmpty)
            {
                titleBuilder.Append(Run.GameName);

                if (!categoryEmpty)
                {
                    if (!gameNameEmpty)
                        titleBuilder.Append(" - ");
                    titleBuilder.Append(Run.CategoryName);
                }
            }

            var title = titleBuilder.ToString();

            var splitName = "";
            var splitTime = "-";
            var deltaTime = "-";

            if ((State.CurrentPhase == TimerPhase.Running 
                || State.CurrentPhase == TimerPhase.Paused)
                && State.CurrentSplitIndex > 0)
            {
                var lastSplit = State.Run[State.CurrentSplitIndex - 1];

                splitName = lastSplit.Name ?? "";
                splitTime = timeFormatter.Format(lastSplit.SplitTime[State.CurrentTimingMethod]);
                deltaTime = deltaTimeFormatter.Format(lastSplit.SplitTime[State.CurrentTimingMethod] - lastSplit.PersonalBestSplitTime[State.CurrentTimingMethod]);
            }

            var streamLink = "";

            if (notePlaceholder.Contains("$stream"))
            {
                try
                {
                    if (Twitch.Instance.IsLoggedIn || Twitch.Instance.VerifyLogin("", ""))
                    {
                        var userName = Twitch.Instance.ChannelName;
                        streamLink = String.Format("http://twitch.tv/{0}", userName);
                    }
                }
                catch { }
            }

            return notePlaceholder
                .Replace("$game", game)
                .Replace("$category", category)
                .Replace("$title", title)
                .Replace("$pb", pb)
                .Replace("$splitname", splitName)
                .Replace("$splittime", splitTime)
                .Replace("$delta", deltaTime)
                .Replace("$stream", streamLink);
        }

        private void RefreshNotes()
        {
            if (CurrentPlatform == Twitter.Instance)
            {
                ShareSettings.Default.Reload();
                if (State.CurrentPhase == TimerPhase.NotRunning || State.CurrentPhase == TimerPhase.Ended)
                {
                    txtNotes.Text = ShareSettings.Default.TwitterFormat;
                    if (String.IsNullOrEmpty(txtNotes.Text))
                    {
                        txtNotes.Text = "I got a $pb in $title.";
                    }
                }
                else
                {
                    txtNotes.Text = ShareSettings.Default.TwitterFormatRunning;
                    if (String.IsNullOrEmpty(txtNotes.Text))
                    {
                        txtNotes.Text = "I'm $delta in $title.";
                    }
                }
            }
            else if (CurrentPlatform == Twitch.Instance)
            {
                ShareSettings.Default.Reload();
                txtNotes.Text = ShareSettings.Default.TwitchFormat;
                if (String.IsNullOrEmpty(txtNotes.Text))
                {
                    txtNotes.Text = "$title Speedrun";
                }
            }
            else
                txtNotes.Text = "";
        }

        private void SaveNotesFormat()
        {
            if (CurrentPlatform == Twitter.Instance)
            {
                if (State.CurrentPhase == TimerPhase.NotRunning || State.CurrentPhase == TimerPhase.Ended)
                    ShareSettings.Default.TwitterFormat = txtNotes.Text;
                else
                    ShareSettings.Default.TwitterFormatRunning = txtNotes.Text;
                ShareSettings.Default.Save();
            }
            else if (CurrentPlatform == Twitch.Instance)
            {
                ShareSettings.Default.TwitchFormat = txtNotes.Text;
                ShareSettings.Default.Save();
            }
        }

        private void btnSubmit_Click(object sender, EventArgs e)
        {
            var username = txtUser.Text;
            var password = txtPassword.Text;
            var gameName = cbxGame.SelectedItem == null ? "" : cbxGame.SelectedItem.ToString();
            var categoryName = cbxCategory.SelectedItem == null ? "" : cbxCategory.SelectedItem.ToString();
            var version = txtVersion.Text;
            var videoURL = txtVideoURL.Text;
            var notes = FormatNotes(txtNotes.Text);
            var attachSplits = chkAttachSplits.Checked;

            SaveNotesFormat();

            try
            {
                Cursor = Cursors.WaitCursor;

                if (!CurrentPlatform.VerifyLogin(username, password))
                {
                    MessageBox.Show("Your login information seems to be incorrect.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var gameId = CurrentPlatform.GetGameIdByName(gameName);
                var categoryId = CurrentPlatform.GetCategoryIdByName(gameId, categoryName);

                var runSubmitted = CurrentPlatform.SubmitRun(
                    Run,
                    username, password,
                    ScreenShotFunction,
                    attachSplits,
                    State.CurrentTimingMethod,
                    gameId, categoryId,
                    version, notes, videoURL);

                if (runSubmitted)
                {
                    MessageBox.Show(String.Format("Your run was successfully shared to {0}.", CurrentPlatform.PlatformName), "Run Shared", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            MessageBox.Show("The run could not be shared.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            MessageBox.Show(FormatNotes(txtNotes.Text), "Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Insert(String insertText)
        {
            var selectionIndex = txtNotes.SelectionStart;
            txtNotes.Text = txtNotes.Text.Insert(selectionIndex, insertText);
            txtNotes.SelectionStart = selectionIndex + insertText.Length;
            txtNotes.Focus();
        }

        private void btnInsertGame_Click(object sender, EventArgs e)
        {
            Insert("$game");
        }

        private void btnInsertCategory_Click(object sender, EventArgs e)
        {
            Insert("$category");
        }

        private void btnInsertTitle_Click(object sender, EventArgs e)
        {
            Insert("$title");
        }

        private void btnInsertPB_Click(object sender, EventArgs e)
        {
            Insert("$pb");
        }

        private void btnInsertSplitName_Click(object sender, EventArgs e)
        {
            Insert("$splitname");
        }

        private void btnInsertDeltaTime_Click(object sender, EventArgs e)
        {
            Insert("$delta");
        }

        private void btnInsertSplitTime_Click(object sender, EventArgs e)
        {
            Insert("$splittime");
        }

        private void btnInsertStreamLink_Click(object sender, EventArgs e)
        {
            Insert("$stream");
        }
    }
}
