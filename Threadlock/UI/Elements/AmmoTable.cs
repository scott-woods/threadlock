//using Nez;
//using Nez.UI;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Threadlock.Entities.Characters.Player;
//using Threadlock.Entities.Characters.Player.BasicWeapons;
//using Threadlock.UI.Skins;

//namespace Threadlock.UI.Elements
//{
//    public class AmmoTable : Table
//    {
//        Skin _skin;
//        int _fullAmmoSets;
//        int _setsToShow;
//        int _bulletsInLastSet;
//        int _totalSetsNeeded;
//        int _currentAmmo;
//        int _maxAmmo;
//        List<Image> _ammoSets = new List<Image>();

//        Table _baseTable;
//        Table _lowerTable;
//        Table _ammoSetTable;
//        Label _ammoCountLabel;
//        ProgressBar _reloadBar;

//        public AmmoTable(Skin skin, Gun gun)
//        {
//            _skin = skin;
//            HandleGunSetup(gun);
//        }

//        void HandleGunSetup(Gun gun)
//        {
//            gun.OnAmmoCountChanged += OnGunAmmoChanged;
//            gun.OnMaxAmmoChanged += OnGunMaxAmmoChanged;
//            gun.OnReloadStarted += OnGunReloadStarted;

//            _currentAmmo = gun.Ammo;
//            _maxAmmo = gun.MaxAmmo;
//            _fullAmmoSets = gun.MaxAmmo / 5;
//            _setsToShow = gun.Ammo / 5;
//            _bulletsInLastSet = gun.Ammo % 5;
//            _totalSetsNeeded = (int)Math.Ceiling((decimal)gun.MaxAmmo / 5);

//            _baseTable = new Table();
//            Add(_baseTable).Left();

//            _reloadBar = new ProgressBar(_skin, "slider_simple");
//            _reloadBar.SetVisible(false);
//            _baseTable.Add(_reloadBar).Width(Value.PercentWidth(1f, this));

//            _baseTable.Row();

//            _lowerTable = new Table();
//            _baseTable.Add(_lowerTable).Left();

//            _ammoCountLabel = new Label("", _skin, "abaddon_12");
//            _lowerTable.Add(_ammoCountLabel).SetSpaceRight(2f);

//            _ammoSetTable = new Table();
//            _ammoSetTable.Defaults().SetSpaceRight(2f);
//            _lowerTable.Add(_ammoSetTable).Left();

//            UpdateAmmo();
//        }

//        void OnGunAmmoChanged(int ammo)
//        {
//            _currentAmmo = ammo;
//            _setsToShow = ammo / 5;
//            _bulletsInLastSet = ammo % 5;
//            UpdateAmmo();
//        }

//        void OnGunMaxAmmoChanged(int maxAmmo)
//        {
//            _maxAmmo = maxAmmo;
//            _fullAmmoSets = maxAmmo / 5;
//            _totalSetsNeeded = (int)Math.Ceiling((decimal)maxAmmo / 5);
//            UpdateAmmo();
//        }

//        void OnGunReloadStarted(float reloadTime)
//        {
//            _reloadBar.SetMinMax(0, reloadTime);
//            _reloadBar.SetVisible(true);
//            Game1.StartCoroutine(ReloadCoroutine(reloadTime));
//        }

//        IEnumerator ReloadCoroutine(float reloadTime)
//        {
//            var timer = 0f;
//            while (timer < reloadTime)
//            {
//                timer += Time.DeltaTime;
//                _reloadBar.SetValue(Math.Min(reloadTime, timer));
//                yield return null;
//            }
//            _reloadBar.SetVisible(false);
//        }

//        void UpdateAmmo()
//        {
//            //update label
//            _ammoCountLabel.SetText($"{_currentAmmo}/{_maxAmmo}");

//            //remove any unneeded sets if max ammo changed
//            while (_ammoSets.Count > _totalSetsNeeded)
//            {
//                var lastSet = _ammoSets[_ammoSets.Count - 1];
//                lastSet.Remove();
//                _ammoSets.RemoveAt(_ammoSets.Count - 1);
//            }

//            //add missing sets
//            if (_ammoSets.Count < _totalSetsNeeded)
//            {
//                foreach (var ammoSet in _ammoSets)
//                    ammoSet.Remove();
//                _ammoSets.Clear();

//                while (_ammoSets.Count < _totalSetsNeeded)
//                {
//                    var ammoSet = new Image(_skin.GetDrawable("image_ammo_5"));
//                    _ammoSetTable.Add(ammoSet);
//                    _ammoSets.Add(ammoSet);
//                }
//            }

//            //update existing sets
//            for (int i = 0; i < _ammoSets.Count; i++)
//            {
//                var bulletsToShow = i < _setsToShow ? 5 : i == _setsToShow ? _bulletsInLastSet : 0;
//                _ammoSets[i].SetDrawable(_skin.GetDrawable($"image_ammo_{bulletsToShow}"));
//            }
//        }
//    }
//}
