using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PFNSAppRevised
{
    public enum Notation
    {
        Prefix, Postfix
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const int RECTANGLE_HEIGHT = 25;
        public static readonly String[] operations = { "+", "-", "*", "/" };

        private Stack<String> TokenStack;
        private Stack<Grid> GridStack;
        private string[] Tokens;
        private int Token_Counter;
        private int OperatorPopCounter;
        private readonly int StackSize;
        private double AnimationSpeed;
        private bool CurrentlyAnimating;
        private Notation notation;

        public MainWindow()
        {
            InitializeComponent();
            GridStack = new Stack<Grid>();
            TokenStack = new Stack<String>();
            Token_Counter = 0;
            OperatorPopCounter = 0;
            AnimationSpeed = 1;
            CurrentlyAnimating = false;
            notation = Notation.Postfix;
            StackSize = (int)StackCanvas.Height/RECTANGLE_HEIGHT;
        }

        private bool IsValidElement(string elem)
        {
            if (String.IsNullOrEmpty(elem)) return false;
            if (operations.Contains(elem)) return true;
            int num;
            if (Int32.TryParse(elem, out num)) return true;
            return false;
        }

        private void EvaluateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentlyAnimating) //Flag that blocks multiple clicks until the animation is finished.
            {
                if (EquationTextbox.Text.Length == 0)
                    return;
                CurrentlyAnimating = true;
                Tokens = EquationTextbox.Text.Trim().Split(' ');
                if (notation == Notation.Prefix)
                    Array.Reverse(Tokens);
                foreach (string elem in Tokens) //Verify the elements in the equation.
                    if (!IsValidElement(elem))
                    { 
                        MessageBox.Show("Fix your equation, please.");
                        CurrentlyAnimating = false;
                        return; 
                    }
                if (Token_Counter < Tokens.Length)
                    StartEvaluation();
            }
        }

        private void StartEvaluation()
        {
            if (Token_Counter >= Tokens.Length)
            {
                if (TokenStack.Count > 1)
                    MessageBox.Show("You entered too many numbers in the equation!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show("Final Result: " + TokenStack.Pop(), "Result", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                ResetValues();
                return;
            }
            if (TokenStack.Count == StackSize)
            {
                MessageBox.Show("A stack overflow has occured","Overflow",MessageBoxButton.OK,MessageBoxImage.Error);
                ResetValues();
                return;
            }
            TokenStack.Push(Tokens[Token_Counter]);
            Grid ElementGrid = CreateAndDrawGridElement(Tokens[Token_Counter++]);
            CreateAndInvokeAnimation(ElementGrid, ElementGrid.Margin, new Thickness(ElementGrid.Margin.Left, StackCanvas.Height - (RECTANGLE_HEIGHT * GridStack.Count), ElementGrid.Margin.Right, ElementGrid.Margin.Bottom), Push_Completed);
        }

        private Grid CreateAndDrawGridElement(String token)
        {
            Grid ElementGrid = new Grid();
            ElementGrid.Children.Add(new Rectangle()
            {
                Width = StackCanvas.Width,
                Height = RECTANGLE_HEIGHT,
                Margin = new Thickness(0),
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Fill = Brushes.Blue
            });
            ElementGrid.Children.Add(new TextBlock()
            {
                Text = token,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            });
            GridStack.Push(ElementGrid);
            StackCanvas.Children.Add(ElementGrid);
            return ElementGrid;
        }

        private void CreateAndInvokeAnimation(Grid ElementGrid, Thickness StartingPosition, Thickness EndingPosition, EventHandler completedEvent)
        {
            ThicknessAnimation ta = new ThicknessAnimation() { From = StartingPosition, To = EndingPosition, SpeedRatio = AnimationSpeed };
            Storyboard.SetTarget(ta, ElementGrid);
            Storyboard.SetTargetProperty(ta, new PropertyPath(Grid.MarginProperty));
            Storyboard sb = new Storyboard();
            sb.Completed += new EventHandler(completedEvent);
            sb.Children.Add(ta);
            sb.Begin(this);
        }

        private void ResetValues()
        {
            Token_Counter = 0;
            OperatorPopCounter = 0;
            CurrentlyAnimating = false;
            GridStack.Clear();
            TokenStack.Clear();
            StackCanvas.Children.Clear();
        }

        public void Push_Completed(object sender, EventArgs e)
        {
            if (operations.Contains(TokenStack.Peek()))
            {
                int result, op1, op2;
                String op = TokenStack.Pop();
                try
                {
                    op2 = int.Parse(TokenStack.Pop());
                    op1 = int.Parse(TokenStack.Pop());
                    if (op2 == 0 && op == "/")
                    {
                        MessageBox.Show("STOP TRYING TO DIVIDE BY ZERO!");
                        ResetValues();
                        return;
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("You have an incorrect equation.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ResetValues();
                    return;
                }
                switch (op)
                {
                    case "+":
                        result = op1 + op2;
                        break;
                    case "-":
                        if (notation == Notation.Prefix)
                            result = op2 - op1;
                        else
                            result = op1 - op2;
                        break;
                    case "*":
                        result = op1 * op2;
                        break;
                    case "/":
                        if (notation == Notation.Prefix)
                            result = op2 / op1;
                        else
                            result = op1 / op2;
                        break;
                    default:
                        throw new Exception("LOL, BAD OP!");
                }
                TokenStack.Push(result.ToString());
                AnimateOperatorPop();
            }
            else
                StartEvaluation();
        }

        private void AnimateOperatorPop()
        {
            if (OperatorPopCounter == 3)
            {
                OperatorPopCounter = 0;
                Grid ElementGrid = CreateAndDrawGridElement(TokenStack.Peek());
                CreateAndInvokeAnimation(ElementGrid, ElementGrid.Margin, new Thickness(ElementGrid.Margin.Left, StackCanvas.Height - (RECTANGLE_HEIGHT * GridStack.Count), ElementGrid.Margin.Right, ElementGrid.Margin.Bottom), Push_Completed);
                return;
            }
            Grid PoppedGrid = GridStack.Pop();
            CreateAndInvokeAnimation(PoppedGrid,PoppedGrid.Margin,new Thickness(0),Pop_Completed);
            OperatorPopCounter++;
        }
        private void Pop_Completed(object sender, EventArgs e)
        {
            StackCanvas.Children.RemoveAt(StackCanvas.Children.Count - 1);
            AnimateOperatorPop();
        }

        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox1.SelectedIndex == 1)
                notation = Notation.Postfix;
            else
                notation = Notation.Prefix;
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AnimationSpeed = SpeedSlider.Value;
        }
    }
}