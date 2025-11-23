# Componentes Reutilizables

Esta carpeta contiene componentes reutilizables para mejorar la consistencia y mantenibilidad de la aplicación.

## 📦 Componentes Disponibles

### 1. LoadingOverlay
**Archivo**: `LoadingOverlay.xaml`

Overlay de loading con spinner y mensaje que se muestra sobre todo el contenido.

**Uso**:
```xml
<controls:LoadingOverlay />
```

Se vincula automáticamente a `IsBusy` y `Subtitle` del ViewModel.

---

### 2. PointsCard
**Archivo**: `PointsCard.xaml`

Card para mostrar puntos del usuario de forma consistente.

**Propiedades**:
- `Points` (int): Cantidad de puntos
- `Label` (string): Texto descriptivo (default: "Tus puntos")
- `Icon` (string): Emoji o ícono (default: "💰")

**Uso**:
```xml
<controls:PointsCard
    Points="{Binding CurrentUser.Points}"
    Label="Tus puntos disponibles"
    Icon="🌿"/>
```

---

### 3. StatsCard
**Archivo**: `StatsCard.xaml`

Card para mostrar estadísticas numéricas.

**Propiedades**:
- `Value` (int): Valor numérico
- `Label` (string): Etiqueta
- `Icon` (string): Emoji o ícono
- `LabelColor` (Color): Color del label
- `ValueColor` (Color): Color del valor
- `Format` (string): Formato numérico (default: "N0")

**Uso**:
```xml
<controls:StatsCard
    Value="{Binding TotalUsers}"
    Label="Usuarios"
    Icon="👥"
    LabelColor="White"
    ValueColor="White"
    BackgroundColor="{StaticResource Secondary}"/>
```

---

### 4. EmptyStateView
**Archivo**: `EmptyStateView.xaml`

Vista para mostrar cuando no hay datos.

**Propiedades**:
- `Icon` (string): Emoji o ícono (default: "📭")
- `Title` (string): Título principal
- `Message` (string): Mensaje secundario (opcional)

**Uso**:
```xml
<CollectionView ItemsSource="{Binding Items}">
    <CollectionView.EmptyView>
        <controls:EmptyStateView
            Icon="🔍"
            Title="No hay resultados"
            Message="Intenta con otra búsqueda"/>
    </CollectionView.EmptyView>
</CollectionView>
```

---

### 5. SkeletonBox
**Archivo**: `SkeletonBox.xaml`

Componente base para skeleton loading con animación shimmer.

**Uso**:
```xml
<controls:SkeletonBox
    WidthRequest="200"
    HeightRequest="20"
    CornerRadius="4"/>
```

---

### 6. ListItemSkeleton
**Archivo**: `ListItemSkeleton.xaml`

Skeleton pre-diseñado para items de lista con avatar, título, subtítulo y valor.

**Uso**:
```xml
<!-- Mientras se cargan los datos -->
<VerticalStackLayout IsVisible="{Binding IsBusy}">
    <controls:ListItemSkeleton />
    <controls:ListItemSkeleton />
    <controls:ListItemSkeleton />
</VerticalStackLayout>

<!-- Datos reales -->
<CollectionView ItemsSource="{Binding Items}"
                IsVisible="{Binding IsNotBusy}">
    <!-- Items reales -->
</CollectionView>
```

---

### 7. CardSkeleton
**Archivo**: `CardSkeleton.xaml`

Skeleton pre-diseñado para cards genéricas.

**Uso**:
```xml
<VerticalStackLayout IsVisible="{Binding IsBusy}">
    <controls:CardSkeleton />
    <controls:CardSkeleton />
</VerticalStackLayout>
```

---

## 🎨 Mejores Prácticas

### 1. Usar LoadingOverlay para operaciones completas
```xml
<Grid>
    <ScrollView>
        <!-- Contenido -->
    </ScrollView>
    <controls:LoadingOverlay />
</Grid>
```

### 2. Usar Skeletons para cargas incrementales
```xml
<!-- Mostrar skeleton mientras IsBusy = true -->
<VerticalStackLayout IsVisible="{Binding IsBusy}">
    <controls:ListItemSkeleton />
    <controls:ListItemSkeleton />
</VerticalStackLayout>

<!-- Mostrar datos cuando IsNotBusy = true -->
<CollectionView ItemsSource="{Binding Items}"
                IsVisible="{Binding IsNotBusy}">
    <!-- Items reales -->
</CollectionView>
```

### 3. Combinar con EmptyStateView
```xml
<CollectionView ItemsSource="{Binding Items}">
    <CollectionView.EmptyView>
        <controls:EmptyStateView
            Icon="📊"
            Title="No hay datos"
            Message="Los datos aparecerán aquí"/>
    </CollectionView.EmptyView>
</CollectionView>
```

---

## 📊 Beneficios

✅ **Consistencia**: Mismos estilos en toda la app
✅ **Mantenibilidad**: Cambios en un solo lugar
✅ **DRY**: No repetir código
✅ **UX mejorada**: Skeleton loaders dan feedback visual
✅ **Menos líneas de código**: ~70% menos código duplicado

---

## 🚀 Ejemplo Completo

```xml
<views:BasePage xmlns:controls="clr-namespace:AcuPuntos.Views.Controls">
    <Grid>
        <RefreshView Command="{Binding RefreshCommand}">
            <ScrollView>
                <VerticalStackLayout Spacing="20" Padding="20">

                    <!-- Points Card -->
                    <controls:PointsCard
                        Points="{Binding CurrentUser.Points}"
                        Label="Tus puntos"
                        Icon="💰"/>

                    <!-- Stats Grid -->
                    <Grid ColumnDefinitions="*,*" ColumnSpacing="10">
                        <controls:StatsCard
                            Value="{Binding TotalUsers}"
                            Label="Usuarios"
                            Icon="👥"/>
                        <controls:StatsCard
                            Grid.Column="1"
                            Value="{Binding TotalTransactions}"
                            Label="Transacciones"
                            Icon="📊"/>
                    </Grid>

                    <!-- List with Skeleton -->
                    <VerticalStackLayout IsVisible="{Binding IsBusy}">
                        <controls:ListItemSkeleton />
                        <controls:ListItemSkeleton />
                    </VerticalStackLayout>

                    <CollectionView ItemsSource="{Binding Items}"
                                    IsVisible="{Binding IsNotBusy}">
                        <CollectionView.EmptyView>
                            <controls:EmptyStateView
                                Icon="📭"
                                Title="Sin datos"/>
                        </CollectionView.EmptyView>
                        <!-- Items -->
                    </CollectionView>

                </VerticalStackLayout>
            </ScrollView>
        </RefreshView>

        <!-- Loading Overlay -->
        <controls:LoadingOverlay />
    </Grid>
</views:BasePage>
```
