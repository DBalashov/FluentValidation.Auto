# Usage

### Add packages
* FluentValidation
* FluentValidationAuto

### Add configuration in Startup class

```csharp
public sealed class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;
    
    public void ConfigureServices(IServiceCollection services) {
        ... 
        services.AddControllersWithViews(configure)
                .AddAutoValidationErrorFormatter();
        
        // or add custom error formatter 
        // services.AddControllersWithViews(configure)
        //         .AddAutoValidationErrorFormatter((ctx, errors) => new ErrorResult(ctx.TraceIdentifier, "Validation error", Errors: errors));
        
        ...
        var config = services.AddHttpContextAccessor().AddMvcCore();
        ...
        config.AddMvcOptions(o =>
                             {
                                 ...
                                 o.AddAutoValidation(); // <- FluentValidationAuto
                                 ...
                             });
    }
    ...    
}
```

### Add validator as usual for model

```csharp
sealed class UserLoginModelValidator : AbstractValidator<UserLoginModel>
{
    public UserLoginModelValidator()
    {
        RuleFor(f => f.Email).NotEmpty().EmailAddress().MaximumLength(64);
        RuleFor(f => f.Password).NotEmpty().MinimumLength(3).MaximumLength(32);
    }
}
```

### Add attribute `Validator` for model

```csharp
[ExportTsInterface] // I'm use TypeGen for generate TypeScript interfaces from C# models :)
[Validator<UserLoginModelValidator>]
public sealed record UserLoginModel(string Email, string Password);
```

### Thats all

All your models in controllers will automatically validated BEFORE action executed.
It will not reach the Login method execution and validation middleware will return HTTP 400 with errors text or object (depends on configuration above)

```csharp
[HttpPost]
[Route("login")]
public async Task<IActionResult> Login([FromBody] UserLoginModel m) {
    // here the model will already be validated.
    // 
    ...   
}
```
