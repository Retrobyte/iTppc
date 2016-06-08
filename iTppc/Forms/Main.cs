using iTppc.Classes;
using iTppc.Core;
using iTppc.IO;
using iTppc.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace iTppc.Forms
{
    public partial class Main : Form
    {
        private Game _game;
        private Settings _settings;

        public Main(Game g, Settings s)
        {
            InitializeComponent();

            _game = g;
            _settings = s;

            showPokemon();
            resizePics();
            showValues();
            showSettings();

            logTextBox.AppendText(Logger.Log);

            _game.OnActivity += _game_OnActivity;
            _game.OnStatistic += _game_OnStatistic;
        }

        private void _game_OnActivity(string act)
        {
            logTextBox.safeInvoke(() =>
            {
                logTextBox.AppendText(act);
            });
        }

        private void _game_OnStatistic(Statistic stat)
        {
            winsLabel.safeInvoke(() => { winsLabel.Text = string.Format("Wins: {0}", stat.Wins); });
            lossesLabel.safeInvoke(() => { lossesLabel.Text = string.Format("Losses: {0}", stat.Losses); });
            moneyGainedLabel.safeInvoke(() => { moneyGainedLabel.Text = string.Format("Money Gained: ${0:n0}", stat.MoneyGained); });
            teamPointsGainedLabel.safeInvoke(() => { teamPointsGainedLabel.Text = string.Format("Team Points Gained: {0:n0}", stat.TeamPointsGained); });

            showPokemon();
            showValues(stat.MoneyGained, stat.TeamPointsGained);
        }

        private void Main_Shown(object sender, EventArgs e)
        {
            if (_game.Banned)
                MessageBox.Show("Your account has been banned!", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = MessageBox.Show("Are you sure to exit?", string.Empty, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No;
        }

        private void executeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (executeToolStripMenuItem.Text)
            {
                case "Execute":
                    if (_game.Running)
                    {
                        MessageBox.Show("Cannot execute while already battling is already active.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    disableControls(true);

                    string trainerPath = string.Format(@"{0}\trainers.txt", Application.StartupPath);
                    SortedDictionary<int, int> trainers = new SortedDictionary<int, int>();

                    if (File.Exists(trainerPath))
                    {
                        foreach (string trainer in File.ReadLines(trainerPath))
                        {
                            int trainerLevel;
                            int trainerId;
                            string[] parts = trainer.Split(':');

                            if (Int32.TryParse(parts[0], out trainerLevel) && Int32.TryParse(parts[1], out trainerId) && !trainers.ContainsKey(trainerLevel))
                                trainers.Add(trainerLevel, trainerId);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Unable to load trainer file.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        disableControls(false);
                        return;
                    }

                    if (!validateSettings())
                    {
                        MessageBox.Show("The current configuration is invalid.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        disableControls(false);
                        return;
                    }

                    updateSettings();
                    _game.startBattle(_settings, trainers, endCallback);

                    executeToolStripMenuItem.Text = "Terminate";
                    executeToolStripMenuItem.Image = Properties.Resources.cross;

                    break;
                case "Terminate":
                    executeToolStripMenuItem.Enabled = false;
                    
                    _game.stopBattle(endCallback);

                    break;
            }
        }

        private void double_textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.') || (e.KeyChar == '.' && ((TextBox)sender).Text.IndexOf('.') != -1);
        }

        private void int_textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar);
        }

        private void saveConfigurationButton_Click(object sender, EventArgs e)
        {
            updateSettings();
            _settings.save();

            MessageBox.Show("Configuration saved successfully!", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void endCallback()
        {
            this.safeInvoke(() =>
            {
                executeToolStripMenuItem.Enabled = true;
                executeToolStripMenuItem.Text = "Execute";
                executeToolStripMenuItem.Image = Properties.Resources.tick;
            });

            disableControls(false);
        }

        private void resizePics()
        {
            firstPictureBox.safeInvoke(() => { firstPictureBox.Image = _game.Roster[0].Image; firstPictureBox.resizelocate(); });
            secondPictureBox.safeInvoke(() => { secondPictureBox.Image = _game.Roster[1].Image; secondPictureBox.resizelocate(); });
            thirdPictureBox.safeInvoke(() => { thirdPictureBox.Image = _game.Roster[2].Image; thirdPictureBox.resizelocate(); });
            fourthPictureBox.safeInvoke(() => { fourthPictureBox.Image = _game.Roster[3].Image; fourthPictureBox.resizelocate(); });
            fifthPictureBox.safeInvoke(() => { fifthPictureBox.Image = _game.Roster[4].Image; fifthPictureBox.resizelocate(); });
            sixthPictureBox.safeInvoke(() => { sixthPictureBox.Image = _game.Roster[5].Image; sixthPictureBox.resizelocate(); });
        }

        private void showPokemon()
        {
            firstNameLabel.safeInvoke(() => { firstNameLabel.Text = _game.Roster[0].Name; });
            firstLevelLabel.safeInvoke(() => { firstLevelLabel.Text = string.Format("Level: {0}", _game.Roster[0].Level); });
            firstItemLabel.safeInvoke(() => { firstItemLabel.Text = string.Format("Item: {0}", _game.Roster[0].Item); });

            secondNameLabel.safeInvoke(() => { secondNameLabel.Text = _game.Roster[1].Name; });
            secondLevelLabel.safeInvoke(() => { secondLevelLabel.Text = string.Format("Level: {0}", _game.Roster[1].Level); });
            secondItemLabel.safeInvoke(() => { secondItemLabel.Text = string.Format("Item: {0}", _game.Roster[1].Item); });

            thirdNameLabel.safeInvoke(() => { thirdNameLabel.Text = _game.Roster[2].Name; });
            thirdLevelLabel.safeInvoke(() => { thirdLevelLabel.Text = string.Format("Level: {0}", _game.Roster[2].Level); });
            thirdItemLabel.safeInvoke(() => { thirdItemLabel.Text = string.Format("Item: {0}", _game.Roster[2].Item); });

            fourthNameLabel.safeInvoke(() => { fourthNameLabel.Text = _game.Roster[3].Name; });
            fourthLevelLabel.safeInvoke(() => { fourthLevelLabel.Text = string.Format("Level: {0}", _game.Roster[3].Level); });
            fourthItemLabel.safeInvoke(() => { fourthItemLabel.Text = string.Format("Item: {0}", _game.Roster[3].Item); });

            fifthNameLabel.safeInvoke(() => { fifthNameLabel.Text = _game.Roster[4].Name; });
            fifthLevelLabel.safeInvoke(() => { fifthLevelLabel.Text = string.Format("Level: {0}", _game.Roster[4].Level); });
            fifthItemLabel.safeInvoke(() => { fifthItemLabel.Text = string.Format("Item: {0}", _game.Roster[4].Item); });

            sixthNameLabel.safeInvoke(() => { sixthNameLabel.Text = _game.Roster[5].Name; });
            sixthLevelLabel.safeInvoke(() => { sixthLevelLabel.Text = string.Format("Level: {0}", _game.Roster[5].Level); });
            sixthItemLabel.safeInvoke(() => { sixthItemLabel.Text = string.Format("Item: {0}", _game.Roster[5].Item); });
        }

        private void showValues(int moneyGained = 0, int teamPointsGained = 0)
        {
            moneyStripLabel.Text = string.Format("Money: ${0:n0}", _game.Money + moneyGained);
            teamPointsStripLabel.Text = string.Format("Team Points: {0:n0}", _game.TeamPoints + teamPointsGained);
        }

        private bool validateSettings()
        {
            double clickLower = delayLowerTextBox.getDouble(-1);
            double clickUpper = delayUpperTextBox.getDouble(-1);

            if (clickLower < 0 || clickUpper < 0 || clickUpper - clickLower < 0)
                return false;

            int waitLower = waitLowerTextBox.getInt(-1);
            int waitUpper = waitUpperTextBox.getInt(-1);

            if (waitLower < 0 || waitUpper < 0 || waitUpper - waitLower < 0)
                return false;

            int intervalLower = intervalLowerTextBox.getInt(-1);
            int intervalUpper = intervalUpperTextBox.getInt(-1);

            if (intervalLower < 0 || intervalUpper < 0 || intervalUpper - intervalLower < 0)
                return false;

            int pokemonNumber = pokemonNumberTextBox.getInt(-1);

            if (pokemonNumber < 1 || pokemonNumber > 6)
                return false;

            int pokemonLevel = pokemonLevelTextBox.getInt(-1);

            if (pokemonLevel < 5)
                return false;

            return true;
        }

        private void showSettings()
        {
            delayLowerTextBox.safeInvoke(() => { delayLowerTextBox.Text = _settings.ClickLower.ToDecimalString(); });
            delayUpperTextBox.safeInvoke(() => { delayUpperTextBox.Text = _settings.ClickUpper.ToDecimalString(); });
            waitLowerTextBox.safeInvoke(() => { waitLowerTextBox.Text = _settings.WaitLower.ToString(); });
            waitUpperTextBox.safeInvoke(() => { waitUpperTextBox.Text = _settings.WaitUpper.ToString(); });
            intervalLowerTextBox.safeInvoke(() => { intervalLowerTextBox.Text = _settings.IntervalLower.ToString(); });
            intervalUpperTextBox.safeInvoke(() => { intervalUpperTextBox.Text = _settings.IntervalUpper.ToString(); });
            pokemonNumberTextBox.safeInvoke(() => { pokemonNumberTextBox.Text = _settings.PokemonNumber.ToString(); });
            pokemonLevelTextBox.safeInvoke(() => { pokemonLevelTextBox.Text = _settings.PokemonLevel.ToString(); });
            showMessageCheckBox.safeInvoke(() => { showMessageCheckBox.Checked = _settings.ShowMessage; });
            playSoundCheckBox.safeInvoke(() => { playSoundCheckBox.Checked = _settings.PlaySound; });
        }

        private void updateSettings()
        {
            _settings.ClickLower = delayLowerTextBox.getDouble(1.2);
            _settings.ClickUpper = delayUpperTextBox.getDouble(1.6);
            _settings.WaitLower = waitLowerTextBox.getInt(5);
            _settings.WaitUpper = waitUpperTextBox.getInt(10);
            _settings.IntervalLower = intervalLowerTextBox.getInt(60);
            _settings.IntervalUpper = intervalUpperTextBox.getInt(75);
            _settings.PokemonNumber = pokemonNumberTextBox.getInt(1);
            _settings.PokemonLevel = pokemonLevelTextBox.getInt(2499);
            _settings.ShowMessage = showMessageCheckBox.Checked;
            _settings.PlaySound = playSoundCheckBox.Checked;
        }

        private void disableControls(bool disable)
        {
            delayLowerTextBox.safeInvoke(() => { delayLowerTextBox.ReadOnly = disable; });
            delayUpperTextBox.safeInvoke(() => { delayUpperTextBox.ReadOnly = disable; });
            waitLowerTextBox.safeInvoke(() => { waitLowerTextBox.ReadOnly = disable; });
            waitUpperTextBox.safeInvoke(() => { waitUpperTextBox.ReadOnly = disable; });
            intervalLowerTextBox.safeInvoke(() => { intervalLowerTextBox.ReadOnly = disable; });
            intervalUpperTextBox.safeInvoke(() => { intervalUpperTextBox.ReadOnly = disable; });
            pokemonNumberTextBox.safeInvoke(() => { pokemonNumberTextBox.ReadOnly = disable; });
            pokemonLevelTextBox.safeInvoke(() => { pokemonLevelTextBox.ReadOnly = disable; });
            showMessageCheckBox.safeInvoke(() => { showMessageCheckBox.Enabled = !disable; });
            playSoundCheckBox.safeInvoke(() => { playSoundCheckBox.Enabled = !disable; });
        }
    }
}
