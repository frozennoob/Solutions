using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MyNotepad
{
    public partial class MainWindow : Window
    {
        List<Note> items = new List<Note>(); // Этот список будет хранить в себе наши записи

        //Настраиваем переменные подключения к базе данных
        string connectionString = ConfigurationManager.ConnectionStrings["localDB"].ToString();
        string queryString = "SELECT * from notes ORDER BY lastName, firstName, fathersName";

        public MainWindow()
        {
            InitializeComponent();
            items.Add(Note.GetEmpty()); //Создаем пустую запись , она будет служить как для добавления новых записей , так и просто для удобства и красоты.
            // Установка соединения с БД и загрузка данных в программу
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        items.Add(Note.CreateInstance(reader.GetString(0), reader.GetString(1), reader.GetString(2), 
                            reader.GetString(3), reader.GetString(4), reader.GetDateTime(5), ref StatusLabel));
                    }
                    reader.Close();
                    connection.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            notesListBox.ItemsSource = items; // Инициализируем ListBox нашим списком
            notesListBox.SelectedIndex = 0; 
        }


        //В целом по программе можно было бы использовать DataBindings для связывания полей формы 
        //со структурами данных. Но я хотел чтобы пользователь записывал данные в базу 
        //нажатием на кнопку и имел возможность добавлять их через пустое поле в списке.
        //И для этих целей нативный способ показался мне более подходящим.

        //В случае изменения выделенной записи , меняем содержимое соответствующих полей на форме.
        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((Note)notesListBox.SelectedItem) == Note.GetEmpty())
                textBoxLastName.Text = "";
            else
                textBoxLastName.Text = ((Note)notesListBox.SelectedItem).LastName;
            textBoxFirstName.Text = ((Note)notesListBox.SelectedItem).FirstName;
            textBoxFathersName.Text = ((Note)notesListBox.SelectedItem).FathersName;
            textBoxPhone.Text = ((Note)notesListBox.SelectedItem).PhoneNumber;
            textBoxMail.Text = ((Note)notesListBox.SelectedItem).Email;
            if (((Note)notesListBox.SelectedItem).BirthDate == DateTime.MinValue)
                datePicker.Text = "";
            else
                datePicker.Text = ((Note)notesListBox.SelectedItem).BirthDate.ToString().Substring(0, 10);
        }

        // Создаем новую запись перемещая фокус на пустое поле в списке
        private void NewClicked(object sender, RoutedEventArgs e)
        {
            notesListBox.SelectedIndex = 0;
        }
        
        // Сохранение записи в наш список items и в базу данных
        private void SaveClicked(object sender, RoutedEventArgs e)
        {
            //Фамилия единственное обязательное не пустое поле. Проверяем ввел ли ее пользователь
            if (textBoxLastName.Text.Length < 1)
            {
                StatusLabel.Content = "Укажите фамилию";
                return;
            }

            //Соединямся с БД
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command;
                Note tmp = Note.CreateInstance(textBoxLastName.Text, textBoxFirstName.Text, textBoxFathersName.Text, 
                    textBoxPhone.Text, textBoxMail.Text, datePicker.DisplayDate, ref StatusLabel);
                try{
                    connection.Open();
                    // Две ветки , либо это новая запись , либо изменение существующей.Проверяем по индексу.
                    if (notesListBox.SelectedIndex == 0)
                    {
                        items.Add(tmp);
                        command = new SqlCommand(
                            "INSERT INTO dbo.notes (lastName, firstName, fathersName, phoneNumber, email, birthDate)" + 
                            "VALUES (@lastName, @firstName, @fathersName, @phoneNumber, @email, @birthDate)", connection);
                        notesListBox.SelectedItem = tmp;
                    }
                    else
                    {
                        ((Note)notesListBox.SelectedItem).Clone(tmp);
                        command = new SqlCommand(
                            "UPDATE dbo.notes SET lastName = @lastName, firstName = @firstName, fathersName = @fathersName,"  + 
                            "phoneNumber = @phoneNumber, email = @email, birthDate = @birthDate", connection);
                    }
                    command.Parameters.AddWithValue("@lastName", textBoxLastName.Text);
                    command.Parameters.AddWithValue("@firstName", textBoxFirstName.Text);
                    command.Parameters.AddWithValue("@fathersName", textBoxFathersName.Text);
                    command.Parameters.AddWithValue("@phoneNumber", textBoxPhone.Text);
                    command.Parameters.AddWithValue("@email", textBoxMail.Text);
                    command.Parameters.AddWithValue("@birthDate", datePicker.DisplayDate);
                    command.ExecuteNonQuery();
                    connection.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            items.Sort();
            notesListBox.Items.Refresh();
        }

        //Удаляем выделенную запись.
        private void DeleteClicked(object sender, RoutedEventArgs e)
        {
            //Нашу пустую запись удалить нельзя. Проверяем она ли это.
            if (notesListBox.SelectedIndex == 0)
                return;
            //Запрос подтверждения
            if (MessageBox.Show(this, "Вы уверены, что хотите удалить выбранный элемент?" + this.Name.ToString(), "Удалить?", 
                MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.Cancel)
                return;
            //Подключение к БД и удаление
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("DELETE FROM dbo.notes WHERE lastName = @lastName AND " + 
                    "firstName = @firstName AND fathersName = @fathersName", connection);
                try
                {
                    connection.Open();
                    command.Parameters.AddWithValue("@lastName", textBoxLastName.Text);
                    command.Parameters.AddWithValue("@firstName", textBoxFirstName.Text);
                    command.Parameters.AddWithValue("@fathersName", textBoxFathersName.Text);
                    command.ExecuteNonQuery();
                    connection.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            items.Remove((Note)notesListBox.SelectedItem);
            notesListBox.SelectedIndex = 0;
            notesListBox.Items.Refresh();
        }
    }



    //Класс экземпляры которого реализуют наши записи  
    class Note : IComparable<Note>  //Будем  сортировать его в списке по ToString, который переопределен ниже
    {
        private string lastName;
        private string firstName;
        private string fathersName;
        private string phoneNumber;
        private string email;
        private DateTime birthDate;

        //Пустая запись-Singleton для нашего списка
        private static Note Emptynote = new Note("<...>", "", "", "", "", new DateTime());

        public static Note GetEmpty()
        {
            return Emptynote;
        }
        
        //Для создания экзмпляров используюем фабрику, так-как хотим проверить поля на валидность до того, как размещать их с помощью new
        public static Note CreateInstance(string lastName, string firstName, string fathersName, string phoneNumber, string email, DateTime birthDate, ref Label errorOut)
        {
            //Проверяем переданные аргументы на валидность.
            //В случае неудачи выводим текст ошибки в переданную в аргументе метода Label errorOut
            try
            {
                ValidateName(lastName);
                ValidateName(firstName);
                ValidateName(fathersName);
                ValidatePhone(phoneNumber);
                ValidateEmail(email);
                ValidateDate(birthDate);
                errorOut.Content = "";
            }
            catch (ArgumentOutOfRangeException e)
            {
                errorOut.Content = e.Message;
                return null;
            }
            return new Note(lastName, firstName, fathersName, phoneNumber, email, birthDate);
        }
        
        // Функции проверки допустимости значений полей класса
        private static void ValidateName(string name)
        {
            if (!Regex.IsMatch(name, @"^$|^[a-zA-Zа-яА-ЯЁё]+$"))
                throw new ArgumentOutOfRangeException(name);
        }
        private static void ValidatePhone(string name)
        {
            string pattern = @"^$|^((8|\+7)[\- ]?)?(\(?\d{3}\)?[\- ]?)?[\d\- ]{7,10}$";
            if (!Regex.IsMatch(name, pattern))
                throw new ArgumentOutOfRangeException(name);
        }
        private static void ValidateEmail(string name)
        {
            string pattern = @"^$|^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$";
            if (!Regex.IsMatch(name, pattern))
                throw new ArgumentOutOfRangeException(name);
        }
        private static void ValidateDate(DateTime date)
        {
            if (date.ToShortDateString() == "")
                return;
            if (date > DateTime.Today)
                throw new ArgumentOutOfRangeException(date.ToShortDateString());
        }
        
        //Закрытый конструктор от всех - шести параметров
        private Note(string lastName, string firstName, string fathersName, string phoneNumber, string email, DateTime birthDate)
        {
            this.lastName = UpdateString(lastName);
            this.firstName = UpdateString(firstName);
            this.fathersName = UpdateString(fathersName);
            this.birthDate = birthDate;
            this.phoneNumber = phoneNumber;
            this.email = email;
        }

        //Этот метод позволяет скопировать в уже существующую запись данные другой записи.
        //Он нам нужен потому что мы не хотим давать доступ к полям на прямую, так-как им нужна валидация
        public void Clone(Note other)
        {
            this.lastName = other.lastName;
            this.firstName = other.firstName;
            this.fathersName = other.fathersName;
            this.birthDate = other.BirthDate;
            this.phoneNumber = other.phoneNumber;
            this.email = other.email;
        }

        // Форматируем строку , первая буква - заглавная , остальные - строчные
        private string UpdateString(string str)
        {
            if (str.Length < 1)
                return str;
            return str.Substring(0, 1).ToUpper() + str.Substring(1, str.Length - 1).ToLower();
        }

        public override string ToString()
        {
            return LastName + " " + FirstName + " " + FathersName;
        }

        public int CompareTo(Note other)
        {
            if (other == null)
                return -1;
            return ToString().CompareTo(other.ToString());
        }

        public string FirstName
        {
            get
            {
                return firstName;
            }
        }

        public string LastName
        {
            get
            {
                return lastName;
            }
        }

        public string FathersName
        {
            get
            {
                return fathersName;
            }

        }

        public DateTime BirthDate
        {
            get
            {
                return birthDate;
            }

        }

        public string PhoneNumber
        {
            get
            {
                return phoneNumber;
            }
        }

        public string Email
        {
            get
            {
                return email;
            }
        }
    }
}
