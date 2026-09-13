# Datos iniciales FIFA 2026

La migración `20260910025546_SeedWorldCup2026Tournament` crea un escenario reproducible que satisface el mínimo de cuatro equipos, cinco jugadores por equipo, seis partidos y tres resultados.

## Fuente deportiva

Las selecciones, nombres y dorsales proceden de la [lista oficial definitiva de plantillas de FIFA World Cup 2026](https://fdp.fifa.org/assetspublic/ce281/pdf/SquadLists-English.pdf?gsid=28108c2b-581c-401b-b923-1d57c03f007a), publicada por FIFA después de recibir las listas de las 48 selecciones.

Se utiliza una muestra de cinco jugadores por selección:

| Selección | Código | Jugadores y dorsales |
|---|---|---|
| Argentina | ARG | Juan Musso 1, Rodrigo De Paul 7, Julián Álvarez 9, Lionel Messi 10, Cristian Romero 13 |
| Brasil | BRA | Alisson Becker 1, Marquinhos 4, Vinícius Júnior 7, Bruno Guimarães 8, Neymar Jr 10 |
| Francia | FRA | Brice Samba 1, Dayot Upamecano 4, Ousmane Dembélé 7, Aurélien Tchouaméni 8, Kylian Mbappé 10 |
| España | ESP | David Raya 1, Mikel Merino 6, Lamine Yamal 19, Pedri 20, Mikel Oyarzabal 21 |

Las fechas, cruces, goles y marcadores forman un torneo demostrativo creado para probar las reglas del sistema. No representan el calendario ni los resultados reales de la Copa Mundial.

## Torneo demostrativo

Los seis cruces cubren todas las combinaciones posibles entre cuatro equipos:

| Partido | Estado | Marcador |
|---|---|---:|
| Argentina - Brasil | Played | 2-1 |
| Francia - España | Played | 1-1 |
| Argentina - Francia | Played | 0-1 |
| Brasil - España | Scheduled | — |
| Argentina - España | Scheduled | — |
| Brasil - Francia | Scheduled | — |

Los goles finalizados son dos de Lionel Messi y uno de Vinícius Júnior, Kylian Mbappé, Lamine Yamal y Ousmane Dembélé. Cada marcador coincide con la cantidad de goles por equipo.

En una base limpia, la posición resultante es Francia con 4 puntos, Argentina con 3, España con 1 y Brasil con 0. Lionel Messi encabeza los goleadores con dos tantos.

## Convivencia con datos manuales

La migración busca primero las abreviaturas `ARG`, `BRA`, `FRA` y `ESP`. Si alguna ya existe, utiliza el identificador encontrado. También reutiliza un jugador cuando coinciden equipo, nombre, dorsal y estado activo. Los partidos y goles del escenario usan identificadores deterministas y no se duplican.

Los registros creados previamente mediante Swagger permanecen intactos. No impiden aplicar el seed, aunque pueden aumentar `totalRecords` y modificar las estadísticas globales si incluyen partidos `Played` de estas selecciones. Los identificadores reservados del escenario permiten verificar exactamente sus seis partidos y seis goles sin borrar información manual.

El método `Down` elimina primero goles, luego partidos, jugadores y equipos del seed. Conserva cualquier fila que haya adquirido referencias adicionales para evitar eliminar datos creados posteriormente por el usuario.
