# Usage

### Add packages
* FluentValidation
* FluentValidation.Auto

### Add configuration in Startup class
![config.png](images/config.png)

### Add validator as usual for model
![validator.png](images/validator.png)

### Add attribute `Validator` for model
![attribute.png](images/attribute.png)

### Thats all

All your models in controllers will automatically validated BEFORE action executed

![controller.png](images/controller.png)

If model invalid - exception will raised and action will no executed