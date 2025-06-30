import * as Dexie from './dexie/dexie.mjs';
const db = new Dexie.Dexie('TovikTranslate');
db.version(2).stores({
    translations: 'id',
    languages: 'id'
});
export default db;
//# sourceMappingURL=TovikDb.js.map