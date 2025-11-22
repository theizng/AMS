using System;
using System.ComponentModel;

namespace AMS.Models
{
    public class FeeType : INotifyPropertyChanged
    {
        public string FeeTypeId { get; set; } = Guid.NewGuid().ToString("N");

        private string _name = "";
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); } }
        }

        private bool _isRecurring;
        // Định kỳ: khi bật sẽ áp dụng phí này cho TẤT CẢ chu kỳ hiện có (mỗi phòng 1 FeeInstance) nếu chưa có.
        public bool IsRecurring
        {
            get => _isRecurring;
            set { if (_isRecurring != value) { _isRecurring = value; OnPropertyChanged(nameof(IsRecurring)); } }
        }

        private string? _unitLabel;
        public string? UnitLabel
        {
            get => _unitLabel;
            set { if (_unitLabel != value) { _unitLabel = value; OnPropertyChanged(nameof(UnitLabel)); } }
        }

        private decimal _defaultRate;
        public decimal DefaultRate
        {
            get => _defaultRate;
            set { if (_defaultRate != value) { _defaultRate = value; OnPropertyChanged(nameof(DefaultRate)); } }
        }

        private bool _applyAllRooms;
        // Áp dụng tất cả phòng: khi bật sẽ áp dụng phí này vào TẤT CẢ phòng của chu kỳ hiện tại. Khi tắt sẽ gỡ khỏi chu kỳ hiện tại.
        public bool ApplyAllRooms
        {
            get => _applyAllRooms;
            set { if (_applyAllRooms != value) { _applyAllRooms = value; OnPropertyChanged(nameof(ApplyAllRooms)); } }
        }

        private bool _active = true;
        public bool Active
        {
            get => _active;
            set { if (_active != value) { _active = value; OnPropertyChanged(nameof(Active)); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}