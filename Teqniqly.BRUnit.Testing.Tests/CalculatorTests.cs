namespace Teqniqly.BRUnit.Testing.Tests;

/// <summary>
/// A simple test class for the <see cref="Calculator"/> class. This class is just used to get the CI/CD pipeline running.
/// This class will be removed once the real tests are in place.
/// </summary>
public class CalculatorTests
{
    [Fact]
    public void Add_Returns_Correct_Result() => Assert.Equal(10, Calculator.Add(2, 8));
}
