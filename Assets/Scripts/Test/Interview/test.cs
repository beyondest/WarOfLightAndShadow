// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.ComponentModel;
// using System.Linq;
// using System.Runtime.CompilerServices;
// using System.Threading;
// using System.Threading.Tasks;
// using Unity.Jobs;
// using UnityEngine.UIElements;
//
// namespace SparFlame.Test.Interview
// {
//     public class A<T> 
//     {
//         public T? value;
//     }
//
//     public class B : INotifyPropertyChanged,INotifyValueChanged<int>
//     {
//         public event PropertyChangedEventHandler PropertyChanged;
//
//         protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//         {
//             PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//         }
//
//         protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
//         {
//             if (EqualityComparer<T>.Default.Equals(field, value)) return false;
//             field = value;
//             OnPropertyChanged(propertyName);
//             return true;
//         }
//
//         public void SetValueWithoutNotify(int newValue)
//         {
//         }
//
//         public int value { get; set; }
//     }
//
//     public class Person : B
//     {
//         private double height;
//         private double Height
//         {
//             get { return height; }
//             set { SetField(ref height, value, nameof(Height)); }
//         }
//     }
//
//     public class Animal
//     {
//         public string Name{get;set;}
//     }
//
//     public class Dog : Animal
//     {
//         
//     }
//     public class Test
//     {
//         public string name;
//         public int age;
//
//         public string? name2;
//         public void Get<T>(A<T> a, ITest b, B c)
//         {
//             age.ToString();
//             age.ToString();
//             var m = a.value;
//             name = null;
//             name.Length.ToString();
//             name2.Length.ToString();
//             var person = new Person();
//             person.PropertyChanged += OnPropertyChanged;
//
//         }
//         private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
//         {
//             IEnumerable<Animal> animals2 = new List<Animal>();
//             IEnumerable<Dog> dogs = new List<Dog>();
//             var names = dogs.Select(d => d.Name);
//             IEnumerable<Animal> animals = dogs;
//             IInverse<Dog> dog2= new Eatable<Dog>();
//
//             IInverse<Animal> animal2 = new Eatable<Animal>();
//             dog2 = animal2;
//             ComplexDelegate<Derived, Derived, Base> derivedDelegate = (d1, d2) => new Base();
//             ComplexDelegate<Base, Base, Derived> baseDelegate = (b1, b2) => new Derived();
//             ComplexDelegate<Derived, Derived, Base> derivedDelegate2 = baseDelegate;
//
//         }
//
//         public T? Process<T>(T input)
//         {
//             // 这里 T? 是什么意思？
//             // 如果 T 是 int，那么 T? 应该是 Nullable<int>
//             // 如果 T 是 string，那么 T? 应该是 string?
//             // 编译器无法确定！
//             return input;
//         }
//     }
//
//     public enum cond
//     {
//         a,
//         b,
//     }
//
//     public interface ITest
//     {
//         void E()
//         {
//             
//         }
//         public void TEst();
//     }
//
//     public interface C<out T>
//     {
//         public T Get()
//         {
//             return default(T);
//         }
//     }
//     public delegate T Get<out T>();
//
//     public interface IInverse<in T> 
//     {
//         void Eat(T t){}
//     }
//
//     public class Eatable<T> : IInverse<T>
//     {
//         
//     }
//
//     public delegate T ComplexDelegate<in T1, in T2, out T>(T1 arg1, T2 arg2);
//
//     class Base { }
//     class Derived : Base { }
//
//     class IndexGetter
//     {
//         public int this[int index]
//         {
//             get
//             {
//                 return 0;
//             }
//             set
//             {
//                 
//             }
//         }
//     }
//
//     public interface ITEst2 : ITest,IEnumerator
//     {
//         
//     }
//
//     public class testStateMachine : IAsyncStateMachine
//     {
//         public void MoveNext()
//         {
//             throw new NotImplementedException();
//         }
//
//         public void SetStateMachine(IAsyncStateMachine stateMachine)
//         {
//
//         }
//
//         public async Task Task1()
//         {
//             lock (2)
//             {
//                 await Task.Delay(1000);
//             }
//         }
//     }
//     
//
//
// }