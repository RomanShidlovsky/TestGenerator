# TestsGenerator

Необходимо реализовать многопоточный генератор шаблонного кода тестовых классов для одной из библиотек для тестирования (NUnit, xUnit, MSTest) по тестируемым классам.

Входные данные
-------------------
+ Список файлов, для классов из которых необходимо сгенерировать тестовые классы.
+ Путь к папке для записи созданных файлов.
+ Ограничения на секции конвейера (см. далее).

Выходные данные
------------------
+ Файлы с тестовыми классами: в каждом выходном файле должен быть только один тестовый класс, соответствующий одному тестируемому классу, вне зависимости от того, как были расположены тестируемые классы в исходных файлах. 
Например: Input.cs (с классами Foo и Bar) -> FooTests.cs, BarTests.cs.
+ Все сгенерированные тестовые классы должны компилироваться при включении в отдельный проект, в котором имеется ссылка на проект с тестируемыми классами.
+ Все сгенерированные тесты должны завершаться с ошибкой.

Схема работы
--------------
Генерация должна выполняться в конвейерном режиме "производитель-потребитель" и состоять из трех этапов: 
1. параллельная загрузка исходных текстов в память (с ограничением количества файлов, загружаемых за раз);
2. генерация тестовых классов в многопоточном режиме (с ограничением максимального количества одновременно обрабатываемых задач); 
3. параллельная запись результатов на диск (с ограничением количества одновременно записываемых файлов).

При реализации использовать async/await и асинхронный API. Для реализации конвейера использовать Dataflow API.

Главный метод генератора должен возвращать Task и не выполнять никаких ожиданий внутри (блокирующих вызовов task.Wait(), task.Result, etc). Для ввода-вывода также необходимо использовать асинхронный API.

Пример исходного класса и сгенерированных тестов для NUnit:
`C#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MyCode;

namespace MyCode.Tests
{
    [TestFixture]
    public class MyClassTests
    {
        [Test]
        public void FirstMethodTest()
        {
            Assert.Fail("autogenerated");
        }

        [Test]
        public void SecondMethodTest()
        {
            Assert.Fail("autogenerated");
        }
        
        [Test]
        public void ThirdMethod1Test()
        {
            Assert.Fail("autogenerated");
        }
        
        [Test]
        public void ThirdMethod2Test()
        {
            Assert.Fail("autogenerated");
        }
    }
}`
