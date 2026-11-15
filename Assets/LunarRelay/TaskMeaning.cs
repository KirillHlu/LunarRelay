namespace LunarRelay {
public sealed partial class GameSession {
 string TaskMeaning(StationAction target){switch(target.Kind){
 case ActionKind.CleanPanel:return UI("Очистите отмеченную солнечную панель.","Clean the marked solar panel.");
 case ActionKind.Greenhouse:return UI("Соберите созревший урожай в жилом модуле.","Harvest the mature crops in the living module.");
 case ActionKind.Pack:return UI("Принесите упаковку воды в убежище.","Bring a water pack to the shelter.");
 case ActionKind.InstallPack:return UI("Закрепите воду в отмеченном месте убежища.","Secure the water in the marked shelter slot.");
 case ActionKind.Rover:return UI("Отключите зарядку ровера.","Turn off rover charging.");
 case ActionKind.Tool:return UI("Возьмите шуруповёрт в жилом модуле.","Collect the torque driver from the living module.");
 case ActionKind.Air:return Sim.AirOn?UI("Отключите очиститель перед обслуживанием.","Isolate the air cleaner before service."):UI("Включите очиститель воздуха.","Restore air-cleaner power.");
 case ActionKind.Diagnose:return !Sim.FilterChecked?UI("Проверьте состояние очистителя воздуха.","Inspect the air cleaner."):UI("Обслужите стойку очистителя шуруповёртом.","Service the air-cleaner rack with the driver.");
 case ActionKind.Cartridge:return UI("Возьмите запасной картридж очистителя.","Collect the spare air-cleaner cartridge.");
 case ActionKind.RepairAir:return UI("Установите новый картридж в разъём очистителя.","Fit the replacement cartridge in the air-cleaner socket.");
 case ActionKind.Main:return Sim.BreakerTripped?UI("Восстановите автомат главной шины.","Reset the main bus breaker."):Sim.MainPower?UI("Отключите главную шину перед ремонтом массива.","Isolate main power before array service."):UI("Восстановите главную шину.","Restore main power.");
 case ActionKind.RepairArray:return UI("Закрепите соединение солнечного массива шуруповёртом.","Secure the solar-array connection with the driver.");
 case ActionKind.DoorShelter:return UI("Укройтесь в защищённом отсеке.","Take cover in the storm shelter.");
 default:return L(target.Label);
 }}
}
}
